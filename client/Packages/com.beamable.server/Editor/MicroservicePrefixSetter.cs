using Beamable.Server.Editor.DockerCommands;
using Beamable.Editor.UI.Model;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [InitializeOnLoadAttribute]
   public class MicroservicePrefixSetter
   {
      // register an event handler when the class is initialized
      static MicroservicePrefixSetter()
      {
         EditorApplication.playModeStateChanged += LogPlayModeState;
      }

      private static async void LogPlayModeState(PlayModeStateChange state)
      {
         if (DockerCommand.DockerNotInstalled) return;

         try
         {
            foreach (var service in Microservices.ListMicroservices())
            {
               var command = new CheckImageCommand(service)
               {
                  WriteLogToUnity = false
               };
               var isRunning = await command.Start();
               if (isRunning)
               {
                  MicroserviceIndividualization.UseServicePrefix(service.Name);
               }
               else
               {
                  if (state == PlayModeStateChange.EnteredPlayMode)
                  {
                      MicroserviceLogHelper.HandleLog(service, LogLevel.INFO, BeamableLogConstants.UsingRemoteServiceMessage,
                          MicroserviceConfiguration.Instance.LogWarningLabelColor, true, "remote_icon");
                  }

                  MicroserviceIndividualization.ClearServicePrefix(service.Name);
               }
            }
         }
         catch (DockerNotInstalledException)
         {
            // purposefully do nothing... If docker isn't installed; do nothing...
         }
      }
   }
}