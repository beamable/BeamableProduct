using Beamable;
using Beamable.Common;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Core.Platform.SDK;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class AssetStoreTools
{
	private static readonly string[] Packages = new[] {"Packages/com.beamable", "Packages/com.beamable.server"};
	private const string PackageJsonFileName = "package.json";
	private const string BuildDirectory = "Assets/AssetStoreTooling/Build";
	private const string TempDirectory = "Assets/AssetStoreTooling/~";

	private const string EmptyVersionJson = @"""version"": ""0.0.0""";

	public enum EnvironmentType
	{
		DEV, STAGE, PROD
	}

	public static EnvironmentType Environment = EnvironmentType.STAGE;
	public static string VersionString = "0.1.0";
	public static bool Vsp = true;

	[MenuItem("Asset Store Tools/Build")]
	public static void BuildFlow()
	{
		var win = EditorWindow.GetWindow<Window>();
		win.ShowUtility();
	}

	public static void BuildPackage()
	{
		try
		{
			// AssetDatabase.StartAssetEditing();
			if (Directory.Exists(BuildDirectory))
			{
				Directory.Delete(BuildDirectory, true);
			}

			if (Directory.Exists(TempDirectory))
			{
				Directory.Delete(TempDirectory, true);
			}

			Directory.CreateDirectory(BuildDirectory);
			Directory.CreateDirectory(TempDirectory);

			var unityPackagePath = Path.Combine(TempDirectory, "Intermediate.unitypackage");
			var buildZipPath = Path.Combine(BuildDirectory, "Beamable.unitypackage");
			var fullUnityPackagePack = Path.GetFullPath(unityPackagePath);
			var fullZipPath = Path.GetFullPath(buildZipPath);
			var fullTempPath = Path.GetFullPath(Path.Combine(TempDirectory, "temp"));

			Debug.Log("[1/6] Exporting package...");
			AssetDatabase.ExportPackage(Packages, unityPackagePath, ExportPackageOptions.Recurse);

			Debug.Log("[2/6] Extracting contents...");
			ExtractZip(fullUnityPackagePack, fullTempPath);

			Debug.Log("[3/6] Overwriting env-defaults...");
			OverwriteEnvDefaults();

			Debug.Log("[4/6] Overwriting package.json files...");
			OverwritePackageJson();

			Debug.Log("[5/6] Writing final package file...");
			WriteZip(fullZipPath, fullTempPath);

			Debug.Log("[6/6] Cleaning intermediate files...");
			Directory.Delete(TempDirectory, true);
			AssetDatabase.Refresh();
		}
		finally
		{
			// AssetDatabase.StopAssetEditing();
		}
	}

	private static void OverwriteEnvDefaults()
	{
		var envDefaultsPath = BeamableEnvironment.FilePath;
		var sourceEnvPath = GetEnvFilePath(Environment);

		var guid = AssetDatabase.AssetPathToGUID(envDefaultsPath);
		var sourceEnv = File.ReadAllText(sourceEnvPath);
		sourceEnv = sourceEnv.Replace(Constants.Environment.BUILD__SDK__VERSION__STRING, VersionString);
		sourceEnv = sourceEnv.Replace(Constants.Environment.UNITY__VSP__UID, Vsp ? "true" : "false");
		File.WriteAllText(Path.GetFullPath(Path.Combine(TempDirectory, "temp", guid, "asset")),sourceEnv);
	}

	private static void OverwritePackageJson()
	{
		foreach (var package in Packages)
		{
			var packagePath = Path.Combine(package, PackageJsonFileName);
			var guid = AssetDatabase.AssetPathToGUID(packagePath);
			var outputPath = Path.Combine(TempDirectory, "temp", guid, "asset");
			var content = File.ReadAllText(outputPath);
			content = content.Replace(EmptyVersionJson, $@"""version"": ""{VersionString}""");
			File.WriteAllText(outputPath, content);
		}
	}

	private static void ExtractZip(string fullBuildPath, string fullTempPath)
	{
		using (FileStream stream = new FileStream(fullBuildPath, FileMode.Open))
		{
			using (GZipInputStream zipStream = new GZipInputStream(stream))
			{
				using (TarArchive archive = TarArchive.CreateInputTarArchive(zipStream, Encoding.Default))
				{
					archive.ExtractContents(fullTempPath);
				}
			}
		}
	}

	private static void WriteZip(string fullZipPath, string fullTempPath)
	{
		using (FileStream stream = new FileStream(fullZipPath, FileMode.CreateNew))
		{
			using (GZipOutputStream zipStream = new GZipOutputStream(stream))
			{
				using (TarArchive archive = TarArchive.CreateOutputTarArchive(zipStream))
				{
					archive.RootPath = fullTempPath;
					archive.AddFilesRecursive(fullTempPath);
				}
			}
		}
	}

	// taken from https://github.com/TwoTenPvP/UnityPackager/tree/b4865204d1184654d4613a7b475b94a52a5458d5/UnityPackager
	private static void AddFilesRecursive(this TarArchive archive, string directory)
	{
		string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

		foreach (string filename in files)
		{
			TarEntry entry = TarEntry.CreateEntryFromFile(filename);
			if (archive.RootPath != null && Path.IsPathRooted(filename))
			{
				var full = Path.GetFullPath(filename);
				entry.Name = full.Substring(archive.RootPath.Length, full.Length - archive.RootPath.Length);
			}
			entry.Name = entry.Name.Replace('\\', '/');
			archive.WriteEntry(entry, true);
		}
	}


	private static string GetEnvFilePath(EnvironmentType env)
	{
		var basePath = BeamableEnvironment.FilePath;
		var target = "dev";
		switch (env)
		{
			case EnvironmentType.STAGE:
				target = "staging";
				break;
			case EnvironmentType.PROD:
				target = "prod";
				break;
		}

		return basePath.Replace("env-default", $"env-{target}");
	}


	class Window : EditorWindow
	{
		private void OnGUI()
		{
			EditorGUILayout.LabelField("Build Asset Store .UnityPackage");
			Environment = (EnvironmentType)EditorGUILayout.EnumPopup("Environment", Environment);
			VersionString = EditorGUILayout.TextField("Version String", VersionString);
			Vsp = EditorGUILayout.Toggle("VSP", Vsp);
			if (GUILayout.Button("BUILD"))
			{
				BuildPackage();
			}
		}
	}
}
