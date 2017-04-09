using UnityEngine;
using System.Collections;

public struct TargetParams
{
	public string displayName;
	public string displayVersion;
	public BundleParams[] bundleParamList;

	public struct BundleParams
	{
		public string bundleURL;
		public int version;
	}
}

