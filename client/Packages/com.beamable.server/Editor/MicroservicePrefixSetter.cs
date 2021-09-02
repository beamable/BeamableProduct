using Beamable.Server.Editor.DockerCommands;
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

      private static void LogPlayModeState(PlayModeStateChange state)
      {
         if (DockerCommand.DockerNotInstalled) return;

         try
         {
            foreach (var service in Microservices.ListMicroservices())
            {
               var command = new CheckImageCommand(service);
               command.WriteLogToUnity = false;
               command.Start();
               command.Join();
               if (command.IsRunning)
               {
                  Debug.Log($"Microservice {service.Name} will use local server");
                  MicroserviceIndividualization.UseServicePrefix(service.Name);
               }
               else
               {
                  Debug.Log($"Microservice {service.Name} will use live server");
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