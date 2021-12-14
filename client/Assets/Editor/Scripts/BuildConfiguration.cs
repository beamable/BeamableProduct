using System;
using System.Linq;
using UnityEditor;

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
      BuildPipeline.BuildPlayer(GetActiveScenes(), "/github/workspace/dist/iOS/iOS", BuildTarget.iOS, BuildOptions.None);
   }
   [MenuItem("Beamable/SampleBuild/Development")]
   public static void Development()
   {
      PlayerSettings.iOS.appleDeveloperTeamID = teamId;
      PlayerSettings.applicationIdentifier = "com.beamable.dev";
      PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE") ?? "1";
      BuildActiveTarget();
   }
   [MenuItem("Beamable/SampleBuild/Staging")]
   public static void Staging()
   {
      PlayerSettings.iOS.appleDeveloperTeamID = teamId;
      PlayerSettings.applicationIdentifier = "com.beamable.staging";
      BuildActiveTarget();
   }
   [MenuItem("Beamable/SampleBuild/Production")]
   public static void Production()
   {
      PlayerSettings.iOS.appleDeveloperTeamID = teamId;
      PlayerSettings.applicationIdentifier = "com.beamable.production";
      BuildActiveTarget();
   }
}
