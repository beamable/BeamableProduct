using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Beamable.Config;
using Beamable.Editor.Environment;
using Beamable.Serialization.SmallerJSON;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{

   public class RunImageCommand : DockerCommand
   {
      public const string ENV_CID = "CID";
      public const string ENV_PID = "PID";
      public const string ENV_SECRET = "SECRET";
      public const string ENV_HOST = "HOST";
      public const string ENV_LOG_LEVEL = "LOG_LEVEL";
      public const string ENV_NAME_PREFIX = "NAME_PREFIX";

      private readonly MicroserviceDescriptor _descriptor;
      public int DebugPort { get; }
      public string ImageName { get; set; }
      public string ContainerName { get; set; }

      public string Cid { get; }
      private string Secret { get; set; }
      public string LogLevel { get; }

      public Dictionary<string, string> Environment { get; private set; }

      public RunImageCommand(MicroserviceDescriptor descriptor, string cid, string secret, string logLevel="Information", Dictionary<string, string> env=null)
      {
         _descriptor = descriptor;
         ImageName = descriptor.ImageName;
         ContainerName = descriptor.ContainerName;

         Environment = new Dictionary<string, string>()
         {
            [ENV_CID] = cid,
            [ENV_PID] = ConfigDatabase.GetString("pid"),
            [ENV_SECRET] = secret,
            [ENV_HOST] = BeamableEnvironment.SocketUrl,
            [ENV_LOG_LEVEL] = logLevel,
            [ENV_NAME_PREFIX] = MicroserviceIndividualization.Prefix,
         };

         if (env != null)
         {
            foreach (var kvp in env)
            {
               Environment[kvp.Key] = kvp.Value;
            }
         }


         Cid = cid;
         Secret = secret;
         LogLevel = logLevel;
         DebugPort = MicroserviceConfiguration.Instance.GetEntry(descriptor.Name).DebugData.SshPort;

         // TODO This log can probably be removed as the message is empty
         UnityLogLabel = $"Docker Run {descriptor.Name}";
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

      public string GetEnvironmentString()
      {
         var keys = Environment.Select(kvp => $"--env {kvp.Key}=\"{kvp.Value}\"");
         var envString = string.Join(" ", keys);
         return envString;
      }

      public override string GetCommandString()
      {
         Environment[ENV_NAME_PREFIX] = MicroserviceIndividualization.Prefix;
         var command = $"{DockerCmd} run --rm " +
                          $"-P " +
                          $"-p {DebugPort}:2222  " +
                          $"{GetEnvironmentString()} " +
                          $"--name {ContainerName} {ImageName}";
         return command;
      }

   }
}