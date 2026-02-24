using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public static class GithubActions
{
	public static void OnBuild()
	{
		Debug.Log("ON BUILD TEST XYZ");

		FileUtil.DeleteFileOrDirectory("Assets/Plugins/UnityPurchasing");

		Debug.Log("ON BUILD TEST XYZ2");
	}
}
