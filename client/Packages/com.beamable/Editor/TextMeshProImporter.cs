using Beamable.Common;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	internal static class TextMeshProImporter
	{
		public static bool EssentialsLoaded => null != Resources.Load<TMP_Settings>("TMP Settings");

		public static Promise<Unit> ImportEssentials()
		{
			var promise = new Promise<Unit>();
			void ImportCallback(string packageName)
			{
				if (packageName == "TMP Essential Resources")
				{
#if UNITY_2018_3_OR_NEWER
               SettingsService.NotifySettingsProviderChanged();
#endif
					AssetDatabase.importPackageCompleted -= ImportCallback;
					promise.CompleteSuccess(new Unit());
				}
			}

			string tmpPath = GetTmpEssentialsResourcesPath();
			AssetDatabase.importPackageCompleted += ImportCallback;

			// var tmpPath = packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
			if (File.Exists(tmpPath))
			{
				AssetDatabase.ImportPackage(tmpPath, false);
			}
			return promise;
		}

		private const string RELATIVE_TMP_RESOURCES_PATH = "Package Resources/TMP Essential Resources.unitypackage";

		static string GetTmpEssentialsResourcesPath()
		{
			var packages = new string[]
			{
				$"Packages/com.unity.ugui/{RELATIVE_TMP_RESOURCES_PATH}", 
				$"Packages/com.unity.textmeshpro/{RELATIVE_TMP_RESOURCES_PATH}"
			};

			foreach (var package in packages)
			{
				if (File.Exists(package))
				{
					return package;
				}
			}
			
			
			var packagePath = Path.GetFullPath("Assets/..");
			if (Directory.Exists(packagePath))
			{
				// Search default location for development package
				if (Directory.Exists(packagePath + "/Assets/Packages/com.unity.TextMeshPro/Editor Resources"))
				{
					return packagePath + "/Assets/Packages/com.unity.TextMeshPro";
				}

				// Search for default location of normal TextMesh Pro AssetStore package
				if (Directory.Exists(packagePath + "/Assets/TextMesh Pro/Editor Resources"))
				{
					return packagePath + "/Assets/TextMesh Pro";
				}

				// Search for potential alternative locations in the user project
				string[] matchingPaths =
					Directory.GetDirectories(packagePath, "TextMesh Pro", SearchOption.AllDirectories);
				string path = ValidateLocation(matchingPaths, packagePath);
				if (path != null) 
					return packagePath + path + "/" + RELATIVE_TMP_RESOURCES_PATH;
			}

			return null;
		}
		
	
		static string ValidateLocation(string[] paths, string projectPath)
		{
			for (int i = 0; i < paths.Length; i++)
			{
				// Check if the Editor Resources folder exists.
				if (Directory.Exists(paths[i] + "/Editor Resources"))
				{
					string folderPath = paths[i].Replace(projectPath, "");
					folderPath = folderPath.TrimStart('\\', '/');
					return folderPath;
				}
			}

			return null;
		}
	}
}
