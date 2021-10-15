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

      public static bool HandleMongoLog(StorageObjectDescriptor storage, string data)
      {
         LogLevel ParseMongoLevel(string level)
         {
            switch (level)
            {
               case "I": return LogLevel.INFO;
               case "F": return LogLevel.FATAL;
               case "E": return LogLevel.ERROR;
               case "W": return LogLevel.WARNING;
               default: return LogLevel.DEBUG;
            }
         }

         if (!(Json.Deserialize(data) is ArrayDict jsonDict)) return false;

         var attrs = ((ArrayDict) jsonDict["attr"]);
         var time = ((ArrayDict) jsonDict["t"])["$date"] as string;

         if (DateTime.TryParse(time, out var logDate))
         {
            time = LogMessage.GetTimeDisplay(logDate);
         }

         var logMessage = new LogMessage
         {
            Message = $" Ctx=[{jsonDict["ctx"] as string}] {jsonDict["msg"] as string}",
            Timestamp = time,
            Level = ParseMongoLevel(jsonDict["s"] as string),
            ParameterText = attrs == null
               ? ""
               : string.Join("\n", attrs.Select(kvp => $"{kvp.Key}={Json.Serialize(kvp.Value, new StringBuilder())}")),
            Parameters = new Dictionary<string, object>()
         };

         EditorApplication.delayCall += () =>
         {
            MicroservicesDataModel.Instance.AddLogMessage(storage, logMessage);
         };
         return true;

      }

      public static bool HandleLog(IDescriptor descriptor, string label, string data)
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
            #if !BEAMABLE_LEGACY_MSW
            if (!DateTime.TryParse(timestamp, out var time))
            {
               time = DateTime.Now;
            }
            var logMessage = new LogMessage
            {
               Message = message,
               Parameters = objs,
               ParameterText = objsToString,
               Level = logLevelValue,
               Timestamp = LogMessage.GetTimeDisplay(time)
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
#if !BEAMABLE_LEGACY_MSW
            var logMessage = new LogMessage
            {
               Message = $"{label}: {data}",
               Parameters = new Dictionary<string, object>(),
               ParameterText = "",
               Level = LogLevel.INFO,
               Timestamp = LogMessage.GetTimeDisplay(DateTime.Now)
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


      public static bool HandleLog(MicroserviceDescriptor descriptor, LogLevel logLevel, string message, Color color, bool isBoldMessage, string postfixIcon)
      {
            var logMessage = new LogMessage
            {
                Message = message,
                Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
                IsBoldMessage = isBoldMessage,
                PostfixMessageIcon = postfixIcon,
                MessageColor = color,
                Level = logLevel
            };

            EditorApplication.delayCall += () =>
            {
                MicroservicesDataModel.Instance.AddLogMessage(descriptor, logMessage);
            };

            return true;
      }
    }

   public class FollowLogCommand : DockerCommand
   {
      private readonly IDescriptor _descriptor;
      public string ContainerName { get; }


      public FollowLogCommand(IDescriptor descriptor)
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
