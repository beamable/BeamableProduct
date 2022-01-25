using System;
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

	private static void BuildActiveTarget()
	{
		try
		{
			//Build
			var results = BuildPipeline.BuildPlayer(GetActiveScenes(), "/github/workspace/dist/iOS/iOS", BuildTarget.iOS, BuildOptions.None);

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
		finally
		{
			Debug.Log("The build has finished.");
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
