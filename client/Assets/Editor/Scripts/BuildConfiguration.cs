using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildSampleProject
{
	public readonly static string teamId = "A6C4565DLF";
	private static string[] GetActiveScenes()
	{
		var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
		return scenes;
	}

	private static string GetBuildPathForTarget(BuildTarget target)
	{
		var prefix = "/github/workspace/dist";
		switch (target)
		{
			case BuildTarget.iOS:
				return prefix + "/iOS/iOS";
			case BuildTarget.Android:
				return prefix + "/Android";
			case BuildTarget.StandaloneWindows:
				return prefix + "/StandaloneWindows";
			case BuildTarget.StandaloneOSX:
				return prefix + "/StandaloneOSX";
			case BuildTarget.WebGL:
				return prefix + "/WebGL";
			default:
				throw new Exception(
					$"Invalid Build Target! Cannot get an output directory for this target. Target=[{target}]");
		}
	}

	private static void BuildActiveTarget()
	{
		try
		{
			//Build

			var activeTarget = EditorUserBuildSettings.activeBuildTarget;
			var distPath = GetBuildPathForTarget(activeTarget);
			var results = BuildPipeline.BuildPlayer(GetActiveScenes(), distPath, activeTarget, BuildOptions.None);

			Debug.Log("PSO testing");
			Debug.Log(results.summary.outputPath);
			Debug.Log(results.summary.ToString());
			if (results.summary.result != BuildResult.Succeeded)
			{
				throw new BuildFailedException("Build failed.");
			}

		}
		catch (BuildFailedException e)
		{
			Debug.LogError(e.StackTrace);
			Debug.LogError(e.Message);
			Debug.LogError(e.Data);
		}
	}
	[MenuItem("Beamable/SampleBuild/Development")]
	public static void Development()
	{
		PlayerSettings.iOS.appleDeveloperTeamID = teamId;
		PlayerSettings.applicationIdentifier = "com.beamable.dev";
		PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE") ?? "2";
		PlayerSettings.bundleVersion = "1.2.0";
		BuildActiveTarget();
	}
	[MenuItem("Beamable/SampleBuild/Staging")]
	public static void Staging()
	{
		PlayerSettings.iOS.appleDeveloperTeamID = teamId;
		PlayerSettings.applicationIdentifier = "com.beamable.staging";
		PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE") ?? "2";
		PlayerSettings.bundleVersion = "1.2.0";
		BuildActiveTarget();
	}
	[MenuItem("Beamable/SampleBuild/Production")]
	public static void Production()
	{
		PlayerSettings.iOS.appleDeveloperTeamID = teamId;
		PlayerSettings.applicationIdentifier = "com.beamable.production";
		PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE") ?? "1";
		PlayerSettings.bundleVersion = "1.2.0";
		BuildActiveTarget();
	}
}
