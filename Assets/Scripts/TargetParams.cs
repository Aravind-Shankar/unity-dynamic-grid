using UnityEngine;
using System.Collections;

// Fully customizable struct for holding all parameters needed to create a DynamicGrid button.
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

