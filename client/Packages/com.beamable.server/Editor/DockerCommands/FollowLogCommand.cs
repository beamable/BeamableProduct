using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beamable.Editor.Microservice.UI;
using Beamable.Editor.UI.Model;
using Beamable.Serialization.SmallerJSON;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
   public static class MicroserviceLogHelper
   {

      public static bool HandleLog(MicroserviceDescriptor descriptor, string label, string data)
      {
         if (Json.Deserialize(data) is ArrayDict jsonDict)
         {
            // this is a serilog message!

            var timestamp = string.Empty;
            var logLevel = "Info"; // info by default
            var message = ""; // rendered message
            var objs = new Dictionary<string, object>();
            foreach (var kvp in jsonDict)
            {
               var key = kvp.Key;
               if (key.StartsWith("__"))
               {
                  switch (key.Substring("__".Length))
                  {
                     case "l": // logLevel
                        logLevel = kvp.Value.ToString();
                        break;
                     case "t": // timestamp
                        timestamp = kvp.Value.ToString();
                        break;
                     case "m": // message
                        message = kvp.Value.ToString();
                        break;
                  }
               }
               else
               {
                  objs.Add(key, kvp.Value);
               }
            }

            string WithColor(Color logColor, string log)
            {
               if (!MicroserviceConfiguration.Instance.ColorLogs) return log;

               var msg = $"<color=\"#{ColorUtility.ToHtmlStringRGB(logColor)}\">{log}</color>";
               return msg;
            }

            var color = Color.grey;
#pragma warning disable 219
            var logLevelValue = LogLevel.DEBUG;
#pragma warning restore 219
            switch (logLevel)
            {
               case "Debug":
                  color = MicroserviceConfiguration.Instance.LogDebugLabelColor;
                  logLevelValue = LogLevel.DEBUG;
                  break;
               case "Warning":
                  color = MicroserviceConfiguration.Instance.LogWarningLabelColor;
                  logLevelValue = LogLevel.WARNING;
                  break;
               case "Info":
                  color = MicroserviceConfiguration.Instance.LogInfoLabelColor;
                  logLevelValue = LogLevel.INFO;
                  break;
               case "Error":
                  color = MicroserviceConfiguration.Instance.LogErrorLabelColor;
                  logLevelValue = LogLevel.ERROR;
                  break;
               case "Fatal":
                  color = MicroserviceConfiguration.Instance.LogFatalLabelColor;
                  logLevelValue = LogLevel.FATAL;
                  break;
               default:
                  color = Color.black;
                  break;
            }

            var f = .8f;
            var darkColor = new Color(color.r * f, color.g * f, color.b * f);

            var objsToString = string.Join("\n", objs.Select(kvp => $"{kvp.Key}={Json.Serialize(kvp.Value, new StringBuilder())}"));

            // report the log message to the right bucket.
            #if BEAMABLE_NEWMS
            var logMessage = new LogMessage
            {
               Message = message,
               Parameters = objs,
               ParameterText = objsToString,
               Level = logLevelValue,
               Timestamp = DateTime.Parse(timestamp)
            };
            EditorApplication.delayCall += () =>
            {
               MicroservicesDataModel.Instance.AddLogMessage(descriptor, logMessage);
            };
            if (MicroserviceConfiguration.Instance.ForwardContainerLogsToUnityConsole)
            {
               Debug.Log($"{WithColor(Color.grey, $"[{label}]")} {WithColor(color, $"[{logLevel}]")} {WithColor(darkColor, $"{message}\n{objsToString}")}");
            }
            #else
            Debug.Log($"{WithColor(Color.grey, $"[{label}]")} {WithColor(color, $"[{logLevel}]")} {WithColor(darkColor, $"{message}\n{objsToString}")}");
            #endif


            return true;
         } else
         {
#if BEAMABLE_NEWMS
            var logMessage = new LogMessage
            {
               Message = $"{label}: {data}",
               Parameters = new Dictionary<string, object>(),
               ParameterText = "",
               Level = LogLevel.INFO,
               Timestamp = DateTime.Now
            };
            EditorApplication.delayCall += () =>
            {
               MicroservicesDataModel.Instance.AddLogMessage(descriptor, logMessage);
            };
            return !MicroserviceConfiguration.Instance.ForwardContainerLogsToUnityConsole;
#else
            return false;
#endif
         }
      }

   }


   public class FollowLogCommand : DockerCommand
   {
      private readonly MicroserviceDescriptor _descriptor;
      public string ContainerName { get; }


      public FollowLogCommand(MicroserviceDescriptor descriptor)
      {
         _descriptor = descriptor;
         ContainerName = descriptor.ContainerName;
      }

      protected override void HandleStandardOut(string data)
      {
         if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardOut(data);
         }
      }

      protected override void HandleStandardErr(string data)
      {
         if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardErr(data);
         }
      }

      public override string GetCommandString()
      {
         return $"{DockerCmd} logs {ContainerName} -f --since 0m";
      }
   }
}