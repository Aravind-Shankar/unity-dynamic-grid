using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DynamicGridController : MonoBehaviour
{
	public GridLayoutGroup targetGrid;		// The GridLayoutGroup to be made dynamic
											// Note that, for ClearGrid to work as-is, this grid is not intended to
											// have any child GameObjects

	public GameObject gridButtonPrefab;		// The prefab for the buttons in the grid

	public string listURL;					// The *full* URL of the webpage that provides the scene/bundle list.

	public string contentRootURL;			// The *incomplete* URL of the webpage that, once filled, will provide
											// the individual AssetBundles.
											// This URL should end with a "/", and on suffixing the exact name of
											// an AssetBundle, it should form a full URL that downloads that bundle.

	IEnumerator Start()
	{
		Caching.CleanCache ();

		// synchronously get scene list
		using (WWW listReq = new WWW (listURL))
		{
			yield return listReq;

			if (!string.IsNullOrEmpty (listReq.error))
			{
				throw new UnityException ("Error fetching bundle list: " + listReq.error);
			}

			ClearGrid ();

			// parse response. My test response looks like:
			// "Scene 1:v1.0.0:testbundle|1,testscenebundle|1\nScene 2:v1.0.0:testbundle|1,testscenebundle2|1"

			// this should be modded as needed based on your web API
			// and the TargetParams struct should also be changed as necessary
			string[] sceneList = listReq.text.Split ('\n');

			TargetParams targetParams;
			string[] sceneSplit, bundleSplit;

			foreach (string sceneInfo in sceneList)
			{
				sceneSplit = sceneInfo.Split (':');
				targetParams.displayName = sceneSplit [0];
				targetParams.displayVersion = sceneSplit [1];

				sceneSplit = sceneSplit [2].Split (',');
				targetParams.bundleParamList = new TargetParams.BundleParams[sceneSplit.Length];

				for (int i = 0; i < sceneSplit.Length; ++i)
				{
					bundleSplit = sceneSplit[i].Split ('|');

					targetParams.bundleParamList [i].bundleURL = contentRootURL + bundleSplit [0];
					targetParams.bundleParamList [i].version = int.Parse (bundleSplit [1]);
				}

				// finished parsing the parameters needed to create one button, stored in targetParams
				// now, one button corresponds to all bundles needed to load one scene and its requisite assets
				CreateGridButton (targetParams);
			}
		}
	}

	void ClearGrid()
	{
		// destroys every child of the grid.
		while (targetGrid.transform.childCount > 0)
			DestroyImmediate (targetGrid.transform.GetChild (0).gameObject);
	}

	void CreateGridButton(TargetParams targetParams)
	{
		// create button, set display parameters
		GameObject newButton = (GameObject)Instantiate (gridButtonPrefab);
		newButton.transform.SetParent(targetGrid.transform);
		newButton.GetComponent<RectTransform> ().localScale = Vector3.one;	// apparently CanvasScaler modifies this

		newButton.GetComponentInChildren<Text> ().text =
			targetParams.displayName + " : " + targetParams.displayVersion;

		// handle button click
		Button buttonComponent = newButton.GetComponent<Button> ();
		buttonComponent.onClick.AddListener (
			() => StartCoroutine(HandleButtonClick(targetParams))
		);
	}

	// A sample handler for loading the scene and assets when a button is clicked in the grid.
	// Loops over all the bundles specified for the scene to which the button corresponds, and
	// loads each of them from the cache or downloads them, as necessary.
	// This function then just retrieves all asset names, scene paths.
	// If it gets asset names, it checks for any assembly (".bytes") files and loads in all the contained types.
	// If it gets a scene name, it loads that scene at the end (i.e. after all other bundles have been processed)
	IEnumerator HandleButtonClick(TargetParams targetParams)
	{
		string[] allAssetNames, allScenePaths;
		string nextSceneName = "";
		GameObject test = new GameObject ("TEST TYPES");
		DontDestroyOnLoad (test);

		foreach (TargetParams.BundleParams bundleParams in targetParams.bundleParamList)
		{
			while (!Caching.ready)
				yield return null;
			
			using (
				WWW bundleReq = WWW.LoadFromCacheOrDownload(
					bundleParams.bundleURL, bundleParams.version)
			) {
				yield return bundleReq;

				if (!string.IsNullOrEmpty(bundleReq.error))
				{
					throw new UnityException ("Error fetching specific bundle: " + bundleReq.error);
				}

				allAssetNames = bundleReq.assetBundle.GetAllAssetNames ();
				allScenePaths = bundleReq.assetBundle.GetAllScenePaths ();

				if (allAssetNames.Length > 0)
				{
					Debug.Log ("Asset names in bundle: " + string.Join (",", allAssetNames));
					foreach (string assetName in allAssetNames)
						if (assetName.EndsWith (".bytes", true, System.Globalization.CultureInfo.CurrentCulture))
						{
							TextAsset assemblyText = bundleReq.assetBundle.LoadAsset<TextAsset> (assetName);
							var assembly = Assembly.Load (assemblyText.bytes);
							var types = assembly.GetTypes ();

							if (types == null)
								Debug.LogError ("No types loaded");
							else
							{
								foreach (System.Type type in types)
								{
									var component = test.AddComponent (type);

									// The following part depends on the assembly loaded.
									// For the TestAssemblyy.bytes included with this sample,
									// there exist "public int x", and "public Vector3 r" fields
									// and this code serves as an example of how to use reflection
									// to access these fields using the types loaded from the assembly.

									/* var xFieldInfo = type.GetField ("x", BindingFlags.Instance | BindingFlags.Public);
									var rFieldInfo = type.GetField ("r", BindingFlags.Instance | BindingFlags.Public);

									Debug.Log ("x value = " + (int)(xFieldInfo.GetValue (component)));
									Debug.Log ("r value = " + (Vector3)(rFieldInfo.GetValue (component))); */
								}
							}
						}
				}
				if (allScenePaths.Length > 0)
				{
					Debug.Log("Scene paths in bundle: " + string.Join(",", allScenePaths));
					if (nextSceneName == "")
					{
						nextSceneName = allScenePaths [0];
					}
				}
			}	
		}

		if (nextSceneName != "")
			SceneManager.LoadScene (nextSceneName);
	}
}
