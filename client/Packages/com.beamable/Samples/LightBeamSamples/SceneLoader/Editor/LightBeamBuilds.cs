
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Windows;
using File = System.IO.File;

public class LightBeamBuilds
{
	[Serializable]
	public class LightBeamRealmConfigIndex
	{
		public List<LightBeamSceneEntry> scenes = new List<LightBeamSceneEntry>();
	}

	[Serializable]
	public class LightBeamSceneEntry
	{
		public string title;
		public string sceneName;
		public string about;
		public bool includeInToc;
		public string realmConfigFile;
	}
	
	
	[MenuItem("LightBeam/Check Config")]
	public static void CheckConfig()
	{
		// var configPath = "Packages/com.beamable/Samples/LightBeamSamples/SceneLoader/Resources/SceneConfig.asset";
		// var config = AssetDatabase.LoadAssetAtPath<LightBeamSceneConfigObject>(configPath);
		var config = Resources.Load<LightBeamSceneConfigObject>("SceneConfig");
		// var config = AssetDatabase.LoadAssetAtPath<LightBeamSceneConfigObject>("Assets/LightBeamBuilds/Resources/SceneConfig.asset");

		// var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/LightBeamBuilds/Resources/SceneConfig.asset");
		var assets = AssetDatabase.FindAssets($"t:{typeof(LightBeamSceneConfigObject).FullName}");
		Debug.Log("FOUND : " + assets.Length);
		Debug.Log("LIGHTBEAM_CONFIG " + (config?.name ?? "<null>"));
		// Debug.Log("LIGHTBEAM_CONFIG " + (asset?.GetType().Name ?? "<no type>"));
	}

	[MenuItem("LightBeam/Build Index")]
	public static void BuildRealmConfigIndex()
	{
		var config = Resources.Load<LightBeamSceneConfigObject>("SceneConfig");
		if (config == null)
		{
			throw new Exception("LIGHTBEAM_NO_CONFIG");
		}

		BuildRealmConfigIndex(config);
	}
	
	public static void BuildRealmConfigIndex(LightBeamSceneConfigObject config)
	{
		var json = JsonUtility.ToJson(new LightBeamRealmConfigIndex
		{
			scenes = config.editorScenes.Select(x => new LightBeamSceneEntry
			{
				title = x.title,
				sceneName = x.scene.name,
				about = x.about,
				includeInToc = x.includeInToc,
				realmConfigFile = x.realmRequirements == null ? null : x.realmRequirements.name
			}).ToList()
		}, true);
		
		FileUtil.DeleteFileOrDirectory("Assets/StreamingAssets/RealmConfigs");
		Directory.CreateDirectory("Assets/StreamingAssets");
		FileUtil.CopyFileOrDirectory("Assets/Minis/RealmConfigs", "Assets/StreamingAssets/RealmConfigs");
		File.WriteAllText("Assets/StreamingAssets/index.json", json);
	}

	[MenuItem("LightBeam/Build All")]
	public static void BuildAll()
	{
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		var config = Resources.Load<LightBeamSceneConfigObject>("SceneConfig");
		if (config == null)
		{
			throw new Exception("LIGHTBEAM_NO_CONFIG");
		}
		Debug.Log("LIGHTBEAM_CONFIG " + config.name);

		var args = Environment.GetCommandLineArgs();
		BuildRealmConfigIndex(config);
		Debug.Log("LIGHTBEAM_ARGS " + string.Join(",", args));
		string outputDir = null;
		for (var i = 0; i < args.Length - 1; i++)
		{
			switch (args[i])
			{
				case "-LIGHTBEAM_BUILD_PATH":
					outputDir = args[i + 1];
					break;
			}
		}

		//If there was no outputDir coming from args, then use the default
		if (string.IsNullOrEmpty(outputDir))
		{
			outputDir = "dist";
		}

		Debug.Log("LIGHTBEAM_OUTPUT " + outputDir);

		var scenePaths = config.scenes.Select(x => x.scenePath).ToList();
		Debug.Log("LIGHTBEAM_SCENE_PATHS " + string.Join(", ", scenePaths));
		BuildOptions options;
		
		// Save the original defines so you can restore them later
		string originalDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);

		// Add your custom defines (semicolon-separated)
		string customDefines = originalDefines + ";BEAM_LIGHTBEAM";

		PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, customDefines);

		
#if UNITY_EDITOR
		options = BuildOptions.AutoRunPlayer;
#else
		options = BuildOPtions.None;
#endif

		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
		{
			scenes = scenePaths.ToArray(),
			locationPathName = outputDir,
			target = BuildTarget.WebGL,
			options = options,
			
		};

		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
		PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, originalDefines);

		BuildSummary summary = report.summary;

		if (summary.result == BuildResult.Succeeded)
		{
			Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
		}

		if (summary.result == BuildResult.Failed)
		{
			Debug.Log("Build failed");
		}

	}
}
