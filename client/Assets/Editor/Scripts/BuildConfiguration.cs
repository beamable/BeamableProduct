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

	private static string GetBaseBuildPath()
	{
		string path = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
		path = string.IsNullOrEmpty(path) ? "/github/workspace/dist" : Path.Combine(path, "dist");

		return path;
	}

	private static string GetBuildPathForTarget(BuildTarget target, string prefix)
	{
		switch (target)
		{
			case BuildTarget.iOS:
				return Path.Combine(prefix, "iOS", "iOS");
			case BuildTarget.Android:
				return Path.Combine(prefix, "Android");
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return Path.Combine(prefix, "StandaloneWindows");
			case BuildTarget.StandaloneOSX:
				return Path.Combine(prefix, "StandaloneOSX");
			case BuildTarget.WebGL:
				return Path.Combine(prefix, "WebGL");
			default:
				throw new Exception(
					$"Invalid Build Target! Cannot get an output directory for this target. Target=[{target}]");
		}
	}

	private static void BuildActiveTarget()
	{
		try
		{
			// Clean first
			var basePath = GetBaseBuildPath();
			if (Directory.Exists(basePath))
			{
				Directory.Delete(basePath, true);
			}

			//Build
			var target = EditorUserBuildSettings.activeBuildTarget;
			var path = GetBuildPathForTarget(target, basePath);
			var results = BuildPipeline.BuildPlayer(GetActiveScenes(), path, target, BuildOptions.None);

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
#if UNITY_6000_0_OR_NEWER
		PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Debug);
		PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
#elif UNITY_2022_3_OR_NEWER
		PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Debug);
		PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
#endif
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
