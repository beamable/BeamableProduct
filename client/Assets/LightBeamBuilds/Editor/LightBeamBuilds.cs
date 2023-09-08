using NUnit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class LightBeamBuilds
{

	public static void BuildAll()
	{
		var configPath = "Packages/com.beamable/Samples/LightBeamSamples/SceneLoader/SceneConfig.asset";
		var config = AssetDatabase.LoadAssetAtPath<LightBeamSceneConfigObject>(configPath);
		Debug.Log("LIGHTBEAM_CONFIG " + (config?.name ?? "<null>"));
		var args = Environment.GetCommandLineArgs();

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

		var scenePaths = config.scenes.Select(x => x.scenePath).ToList();
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
		{
			scenes = scenePaths.ToArray(),
			locationPathName = outputDir,
			target = BuildTarget.WebGL,
			options = BuildOptions.None,
			
		};

		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
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
	
	[MenuItem("LightBeam/Build All")]
	public static void BuildLightBeamProject()
	{

		
		var args = Environment.GetCommandLineArgs();

		Debug.Log("LIGHTBEAM_ARGS " + string.Join(",", args));
		string lightBeamName = null;
		string scenePath = null;
		string outputDir = null;
		for (var i = 0; i < args.Length - 1; i++)
		{
			switch (args[i])
			{
				case "-LIGHTBEAM_NAME":
					lightBeamName = args[i + 1];
					break;
				case "-LIGHTBEAM_SCENE_PATH":
					scenePath = args[i + 1];
					break;
				case "-LIGHTBEAM_BUILD_PATH":
					outputDir = args[i + 1];
					break;
			}
		}
		
		// var lightBeamName = Environment.GetEnvironmentVariable("LIGHTBEAM_NAME");
		// var scenePath = Environment.GetEnvironmentVariable("LIGHTBEAM_SCENE_PATH");
		// var outputDir = Environment.GetEnvironmentVariable("LIGHTBEAM_BUILD_PATH");

		Debug.Log($"LIGHTBEAM NAME=[{lightBeamName}]");
		Debug.Log($"LIGHTBEAM SCENE=[{scenePath}]");
		Debug.Log($"LIGHTBEAM BUILD=[{outputDir}]");
		
		if (string.IsNullOrEmpty(scenePath))
		{
			throw new Exception("no scene specified");
		}
		
		if (string.IsNullOrEmpty(outputDir))
		{
			throw new Exception("no output specified");
		}
		
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
		{
			// scenes = new[] {"Packages/com.beamable/Samples/LightBeamSamples/AccountManager/Sample_AccountManager.unity"},
			scenes = new[] {scenePath},
			locationPathName = outputDir,
			target = BuildTarget.WebGL,
			options = BuildOptions.None,
			
		};

		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
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
