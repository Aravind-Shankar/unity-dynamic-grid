using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DynamicGridController : MonoBehaviour
{

	public GridLayoutGroup targetGrid;
	public GameObject gridButtonPrefab;

	public string listURL;
	public string contentRootURL;

	IEnumerator Start()
	{
		Caching.CleanCache ();

		using (WWW listReq = new WWW (listURL))
		{
			yield return listReq;

			if (!string.IsNullOrEmpty (listReq.error))
			{
				throw new UnityException ("Error fetching bundle list: " + listReq.error);
			}

			ClearGrid ();

			// Parsing response. My test response looks like:
			// "Scene 1:v1.0.0:testbundle|1,testscenebundle|1\nScene 2:v1.0.0:testbundle|1,testscenebundle2|1"
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
				CreateGridButton (targetParams);
			}
		}
	}

	void ClearGrid()
	{
		while (targetGrid.transform.childCount > 0)
			DestroyImmediate (targetGrid.transform.GetChild (0).gameObject);
	}

	void CreateGridButton(TargetParams targetParams)
	{
		GameObject newButton = (GameObject)Instantiate (gridButtonPrefab);
		newButton.transform.SetParent(targetGrid.transform);
		newButton.GetComponent<RectTransform> ().localScale = Vector3.one;	// apparently CanvasScaler modifies this

		newButton.GetComponentInChildren<Text> ().text =
			targetParams.displayName + " : " + targetParams.displayVersion;

		Button buttonComponent = newButton.GetComponent<Button> ();
		buttonComponent.onClick.AddListener (
			() => StartCoroutine(HandleButtonClick(targetParams))
		);
	}

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
									var xFieldInfo = type.GetField ("x", BindingFlags.Instance | BindingFlags.Public);
									var rFieldInfo = type.GetField ("r", BindingFlags.Instance | BindingFlags.Public);

									Debug.Log ("x value = " + (int)(xFieldInfo.GetValue (component)));
									Debug.Log ("r value = " + (Vector3)(rFieldInfo.GetValue (component)));
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
