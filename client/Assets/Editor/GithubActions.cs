using System;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public static class GithubActions
{
#if UNITY_6000_3_OR_NEWER
	[MenuItem("Build/BundleSignedPackage")]
	public static void BundlePackage()
	{
		var BeamableOrgId = Environment.GetEnvironmentVariable("BEAM_ORG_ID");
		var packRequest = Client.Pack("Packages/com.beamable", "build/package_dist", BeamableOrgId);
		Debug.Log("Packing...");
		while (packRequest.Status == StatusCode.InProgress)
		{
			Thread.Sleep(100);
		}

		if (packRequest.Status == StatusCode.Success)
		{
			Debug.Log("packed tarball: " + packRequest.Result.tarballPath);
			return;
		}
		else
		{
			Debug.LogError($"failed to pack. code=[{packRequest.Error.errorCode}] message=[{packRequest.Error.message}]");
		}
	}
#endif
	public static void OnBuild()
	{
		Debug.Log("ON BUILD TEST XYZ");

		FileUtil.DeleteFileOrDirectory("Assets/Plugins/UnityPurchasing");

		Debug.Log("ON BUILD TEST XYZ2");
	}
}
