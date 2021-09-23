using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Config;
using Beamable.Editor.Environment;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
   public class RunStorageCommand : RunImageCommand
   {
      public const string ENV_MONGO_INITDB_ROOT_USERNAME = "MONGO_INITDB_ROOT_USERNAME";
      public const string ENV_MONGO_INITDB_ROOT_PASSWORD = "MONGO_INITDB_ROOT_PASSWORD";
      // public const string ENV_MONGO_INITDB_DATABASE = "MONGO_INITDB_DATABASE";

      public RunStorageCommand(StorageObjectDescriptor storage)
         : base(storage.ImageName, storage.ContainerName)
      {
         // docker run --rm -p 27017:27017 --env MONGO_INITDB_ROOT_USERNAME="admin" --env MONGO_INITDB_ROOT_PASSWORD="admin" --env MONGO_INITDB_DATABASE="admin" --name my_mongo mongo:latest
         var config = MicroserviceConfiguration.Instance.GetStorageEntry(storage.Name);
         Environment = new Dictionary<string, string>
         {
            [ENV_MONGO_INITDB_ROOT_USERNAME] = config.LocalInitUser,
            [ENV_MONGO_INITDB_ROOT_PASSWORD] = config.LocalInitPass,
            // [ENV_MONGO_INITDB_DATABASE] = config.LocalInitDb,
         };

         Ports = new Dictionary<uint, uint>
         {
            [config.LocalDataPort] = 27017
         };
      }
   }

   public class RunStorageToolCommand : RunImageCommand
   {
      public const string ENV_CODE_THEME = "ME_CONFIG_OPTIONS_EDITORTHEME";
      public const string ENV_MONGO_SERVER = "ME_CONFIG_MONGODB_URL";
      public const string ENV_ME_CONFIG_MONGODB_ENABLE_ADMIN = "ME_CONFIG_MONGODB_ENABLE_ADMIN";
      public const string ENV_ME_CONFIG_SITE_COOKIESECRET = "ME_CONFIG_SITE_COOKIESECRET";
      public const string ENV_ME_CONFIG_SITE_SESSIONSECRET = "ME_CONFIG_SITE_SESSIONSECRET";
      public const string RUNNING_STRING = "Mongo Express server listening at http://0.0.0.0:8081";

      public readonly string[] FAILURE_STRINGS = new string[]
      {
         "AuthenticationFailed",
         "UnhandledPromiseRejectionWarning",
         "process with a non-zero exit code"
      };

      public Promise<bool> IsAvailable { get; private set; } = new Promise<bool>();

      public RunStorageToolCommand(StorageObjectDescriptor storage)
      : base(storage.ToolImageName, storage.LocalToolContainerName)
      {
         var config = MicroserviceConfiguration.Instance.GetStorageEntry(storage.Name);
         Environment = new Dictionary<string, string>
         {
            [ENV_CODE_THEME] = "rubyblue",
            [ENV_ME_CONFIG_MONGODB_ENABLE_ADMIN] = "true",
            [ENV_ME_CONFIG_SITE_COOKIESECRET] = Guid.NewGuid().ToString(),
            [ENV_ME_CONFIG_SITE_SESSIONSECRET] = Guid.NewGuid().ToString(),
            [ENV_MONGO_SERVER] = $"mongodb://{config.LocalInitUser}:{config.LocalInitPass}@gateway.docker.internal:{config.LocalDataPort}"
         };
         Ports = new Dictionary<uint, uint>
         {
            [config.LocalUIPort] = 8081
         };

         OnStandardErr += OnMessage;
         OnStandardOut += OnMessage;
      }

      protected override void HandleOnExit()
      {
         if (_exitCode != 0)
         {
            IsAvailable?.CompleteSuccess(false);
         }
         base.HandleOnExit();
      }

      private void OnMessage(string message)
      {
         Debug.Log(message);
         if (message == null) return;
         if (message.Contains(RUNNING_STRING))
         {
            IsAvailable?.CompleteSuccess(true);
         } else if (FAILURE_STRINGS.Any(message.Contains))
         {
            IsAvailable?.CompleteSuccess(false);
         }
      }
   }

   public class RunServiceCommand : RunImageCommand
   {
      public const string ENV_CID = "CID";
      public const string ENV_PID = "PID";
      public const string ENV_SECRET = "SECRET";
      public const string ENV_HOST = "HOST";
      public const string ENV_LOG_LEVEL = "LOG_LEVEL";
      public const string ENV_NAME_PREFIX = "NAME_PREFIX";

      public RunServiceCommand(MicroserviceDescriptor service, string cid, string secret,
         Dictionary<string, string> env) : base(service.ImageName, service.ContainerName, service)
      {
         Environment = new Dictionary<string, string>()
         {
            [ENV_CID] = cid,
            [ENV_PID] = ConfigDatabase.GetString("pid"),
            [ENV_SECRET] = secret,
            [ENV_HOST] = BeamableEnvironment.SocketUrl,
            [ENV_LOG_LEVEL] = "Debug",
            [ENV_NAME_PREFIX] = MicroserviceIndividualization.Prefix,
         };

         if (env != null)
         {
            foreach (var kvp in env)
            {
               Environment[kvp.Key] = kvp.Value;
            }
         }

         var config = MicroserviceConfiguration.Instance.GetEntry(service.Name);
         if (config.IncludeDebugTools)
         {
            Ports = new Dictionary<uint, uint>
            {
               [(uint) config.DebugData.SshPort] = 2222
            };
         }
      }
   }

   public class RunImageCommand : DockerCommand
   {

      private readonly IDescriptor _descriptor;
      public string ImageName { get; set; }
      public string ContainerName { get; set; }

      public Dictionary<string, string> Environment { get; protected set; }
      public Dictionary<uint, uint> Ports { get; protected set; }

      public Action<string> OnStandardOut;
      public Action<string> OnStandardErr;

      public RunImageCommand(string imageName, string containerName,
         IDescriptor descriptor = null,
         Dictionary<string, string> env=null,
         Dictionary<uint, uint> ports=null)
      {
         _descriptor = descriptor;
         ImageName = imageName;
         ContainerName = containerName;

         Environment = env ?? new Dictionary<string, string>();
         Ports = ports ?? new Dictionary<uint, uint>();
      }

      protected override void HandleStandardOut(string data)
      {
         if (_descriptor == null || !MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardOut(data);
         }
         OnStandardOut?.Invoke(data);
      }
      protected override void HandleStandardErr(string data)
      {
         if (_descriptor == null || !MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardErr(data);
         }
         OnStandardErr?.Invoke(data);
      }

      public string GetEnvironmentString()
      {
         var keys = Environment.Select(kvp => $"--env {kvp.Key}=\"{kvp.Value}\"");
         var envString = string.Join(" ", keys);
         return envString;
      }

      string GetPortString()
      {
         var keys = Ports.Select(kvp => $"-p {kvp.Key}:{kvp.Value}");
         var portString = string.Join(" ", keys);
         return portString;
      }

      public override string GetCommandString()
      {
         // Environment[ENV_NAME_PREFIX] = MicroserviceIndividualization.Prefix;
         var command = $"{DockerCmd} run --rm " +
                          $"-P " +
                          $"{GetPortString()} " +
                          $"{GetEnvironmentString()} " +
                          $"--name {ContainerName} {ImageName}";
         return command;
      }

   }
}