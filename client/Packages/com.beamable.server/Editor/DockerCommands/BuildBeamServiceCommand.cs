using System.Diagnostics;
using UnityEditor;

#if BEAMABLE_DEVELOPER
namespace Beamable.Server.Editor.DockerCommands
{
   public class BuildBeamServiceCommand : DockerCommand
   {
#if BEAMABLE_DEVELOPER
       [MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Build Beam Service")]
       public static void Run()
       {
           var command = new BuildBeamServiceCommand();
           command.WriteCommandToUnity = true;
           command.WriteLogToUnity = true;
           command.Start();
       }
#endif

       public BuildBeamServiceCommand()
       {
           UnityLogLabel = "BUILD BEAM";
       }

      protected override void ModifyStartInfo(ProcessStartInfo processStartInfo)
      {
         base.ModifyStartInfo(processStartInfo);
         processStartInfo.EnvironmentVariables["DOCKER_BUILDKIT"] = MicroserviceConfiguration.Instance.EnableDockerBuildkit ? "1" : "0";
      }

      public override string GetCommandString()
      {

#if UNITY_EDITOR_OSX
         return "../microservice/build.sh";
#elif UNITY_EDITOR_WIN
         return "..\\microservice\\build.bat";
#endif

      }
   }
}


#endif