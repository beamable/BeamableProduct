using Beamable.Common;
using Beamable.Editor;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.CodeGen;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.Uploader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Beamable.Server.Editor
{
   public static class Microservices
   {
      private static Dictionary<string, MicroserviceStateMachine> _serviceToStateMachine = new Dictionary<string, MicroserviceStateMachine>();
      private static Dictionary<string, MicroserviceBuilder> _serviceToBuilder = new Dictionary<string, MicroserviceBuilder>();
      private static Dictionary<string, MongoStorageBuilder> _storageToBuilder = new Dictionary<string, MongoStorageBuilder>();

      private static List<MicroserviceDescriptor> _descriptors = null;
      private static List<IDescriptor> _allDescriptors = null;

      private static MicroserviceReflectionCache _MicroserviceReflectionCacheObj;

      static Microservices()
      {
	      _MicroserviceReflectionCacheObj = AssetDatabase.LoadAssetAtPath<MicroserviceReflectionCache>("Packages/com.beamable.server/Editor/ReflectionCache/UserSystems/MicroserviceReflectionCache.asset");
      }
      
      public static List<IDescriptor> AllDescriptors
      {
         get
         {
            if (_allDescriptors != null) return _allDescriptors;
            RefreshDescriptors();
            return _allDescriptors;
         }
      }

      public static List<MicroserviceDescriptor> Descriptors
      {
         get
         {
            if (_descriptors != null) return _descriptors;
            RefreshDescriptors();
            return _descriptors;
         }
      }

      private static List<StorageObjectDescriptor> _storageDescriptors = null;

      public static List<StorageObjectDescriptor> StorageDescriptors
      {
         get
         {
            if (_storageDescriptors != null) return _storageDescriptors;
            RefreshDescriptors();
            return _storageDescriptors;
         }
      }

      public const string SERVICE_PUBLISHED_KEY = "service_published_{0}";

      public static void RefreshDescriptors()
      {
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();

         var dataPath = Application.dataPath;
         var scriptLibraryPath =  dataPath.Substring(0, dataPath.Length - "Assets".Length);

         _descriptors = new List<MicroserviceDescriptor>();
         _storageDescriptors = new List<StorageObjectDescriptor>();
         _allDescriptors = new List<IDescriptor>();

         bool TryGetAttribute<TAttr, TObj>(Type type, out TAttr attr)
            where TAttr : Attribute
         {
            attr = type.GetCustomAttribute<TAttr>(false);
            if (!type.IsClass || attr == null) return false;

            if (!typeof(TObj).IsAssignableFrom(type))
            {
               Debug.LogError(
                  $"The {typeof(TAttr).Name} is only valid on classes that are assignable from {typeof(TObj).Name}");
               return false;
            }

            return true;
         }

         foreach (var assembly in assemblies)
         {
            try
            {
               foreach (var type in assembly.GetTypes())
               {
                  if (TryGetAttribute<StorageObjectAttribute, StorageObject>(type, out var storageAttribute))
                  {
                     if (storageAttribute.StorageName.ToLower().Equals("xxxx"))
                     {
                        continue; // TODO: XXX this is a hacky way to ignore the default microservice...
                     }
                     var descriptor = new StorageObjectDescriptor
                     {
                        Name = storageAttribute.StorageName,
                        Type = type,
                        AttributePath = storageAttribute.SourcePath
                     };
                     _storageDescriptors.Add(descriptor);
                     _allDescriptors.Add(descriptor);
                  }
               }
            }
            catch (Exception)
            {
               continue; // ignore anything that doesn't have a Location property..
            }
         }

         _descriptors = _MicroserviceReflectionCacheObj.Cache.Descriptors;
         _allDescriptors.AddRange(_descriptors);

      }

      public static List<MicroserviceDescriptor> ListMicroservices()
      {
         RefreshDescriptors();
         return _descriptors;
      }

      [DidReloadScripts]
      static void WatchMicroserviceFiles()
      {
         foreach (var service in ListMicroservices())
         {
            GenerateClientSourceCode(service);
            Task.Factory.StartNew(() =>
            {
	            using (var fsw = new FileSystemWatcher(service.SourcePath))
	            {
		            fsw.IncludeSubdirectories = false;
		            fsw.NotifyFilter = NotifyFilters.LastWrite;
		            fsw.Filter = "*.cs";

		            fsw.Changed += (sender, args) =>
		            {
			            GenerateClientSourceCode(service);
		            };
		            fsw.Deleted += (sender, args) =>
		            {
			            /* TODO: Delete the generated client? */
		            };

		            fsw.EnableRaisingEvents = true;
	            }
            });
         }
      }

      public static Promise<ManifestModel> GenerateUploadModel()
      {
         // first, get the server manifest
         return EditorAPI.Instance.FlatMap(de =>
         {
            var client = de.GetMicroserviceManager();
            return client.GetCurrentManifest().Map(manifest =>
            {
               var allServices = new HashSet<string>();

               // make sure all server-side things are represented
               foreach (var serverSideService in manifest.manifest.Select(s => s.serviceName))
               {
                  allServices.Add(serverSideService);
               }

               // add in anything locally...
               foreach (var descriptor in Descriptors)
               {
                  allServices.Add(descriptor.Name);
               }

               // get enablement for each service...
               var entries = allServices.Select(name =>
               {
                   var configEntry = MicroserviceConfiguration.Instance.GetEntry(name);//config.FirstOrDefault(s => s.ServiceName == name);
                   return new ManifestEntryModel
                   {
                     Comment = "",
                     Name = name,
                     Enabled = configEntry?.Enabled ?? true,
                     TemplateId = configEntry?.TemplateId ?? "small",
                   };
               }).ToList();


               var allStorages = new HashSet<string>();

               foreach (var serverSideStorage in manifest.storages.Select(s => s.storageName))
               {
                   allStorages.Add(serverSideStorage);
               }

               foreach (var storageDescriptor in StorageDescriptors)
               {
                   allStorages.Add(storageDescriptor.Name);
               }

               var storageEntries = allStorages.Select(name =>
               {
                  var configEntry = MicroserviceConfiguration.Instance.GetStorageEntry(name);
                  return new StorageEntryModel
                  {
                     Name = name,
                     Type = configEntry?.StorageType ?? "mongov1",
                     Enabled = configEntry?.Enabled ?? true,
                     TemplateId = configEntry?.TemplateId ?? "small",
                  };
               }).ToList();

               return new ManifestModel
               {
                  ServerManifest = manifest.manifest.ToDictionary(e => e.serviceName),
                  Comment = "",
                  Services = entries.ToDictionary(e => e.Name),
                  Storages = storageEntries.ToDictionary(s => s.Name)
               };
            });
         });
      }

      [DidReloadScripts]
      static void AutomaticMachine()
      {
         if (DockerCommand.DockerNotInstalled) return;
         try
         {
            foreach (var d in Descriptors)
            {
               GetServiceStateMachine(d);
            }
         }
         catch (DockerNotInstalledException)
         {
            // do not do anything.
         }
      }

      public static async Promise<Dictionary<string, string>> GetConnectionStringEnvironmentVariables(MicroserviceDescriptor service)
      {
         var env = new Dictionary<string, string>();
         foreach (var reference in service.GetStorageReferences())
         {
            var key = $"STORAGE_CONNSTR_{reference.Name}";
            env[key] = await GetConnectionString(reference, service);
         }

         return env;
      }

      public static Promise<string> GetConnectionString(StorageObjectDescriptor storage, MicroserviceDescriptor user)
      {
         // TODO: Check if the container is actually running. If it isn't, we need to get a connection string to the remote database.
         var config = MicroserviceConfiguration.Instance.GetStorageEntry(storage.Name);
         return Promise<string>.Successful($"mongodb://{config.LocalInitUser}:{config.LocalInitPass}@gateway.docker.internal:{config.LocalDataPort}");
      }

      public static async Promise<bool> OpenLocalMongoTool(StorageObjectDescriptor storage)
      {
         var config = MicroserviceConfiguration.Instance.GetStorageEntry(storage.Name);

         var toolCheck = new CheckImageReturnableCommand(storage.LocalToolContainerName);
         var isToolRunning = await toolCheck.Start(null);

         if (!isToolRunning)
         {
            var run = new RunStorageToolCommand(storage);
            run.Start();
            var success = await run.IsAvailable;
            if (!success)
            {
               return false;
            }
         }

         var path = $"http://localhost:{config.LocalUIPort}";
         Debug.Log($"Opening {path}");
         Application.OpenURL(path);
         return true;
      }

      public static async Promise<bool> SnapshotMongo(StorageObjectDescriptor storage, string destPath)
      {
         var storageCheck = new CheckImageReturnableCommand(storage);
         var isStorageRunning = await storageCheck.Start(null);
         if (!isStorageRunning) return false;

         var dumpCommand = new MongoDumpCommand(storage);
         var dumpResult = await dumpCommand.Start(null);
         if (!dumpResult) return false;

         var cpCommand = new DockerCopyCommand(storage, "/beamable/.", destPath);
         return await cpCommand.Start(null);
      }

      public static async Promise<bool> RestoreMongoSnapshot(StorageObjectDescriptor storage, string srcPath, bool hardReset=true)
      {
         if (hardReset)
         {
            await ClearMongoData(storage);
         }


         var storageCheck = new CheckImageReturnableCommand(storage);
         var isStorageRunning = await storageCheck.Start(null);
         if (!isStorageRunning)
         {
            var restart = new RunStorageCommand(storage);
            restart.Start();
         }

         srcPath += "/."; // copy _contents_ of folder.
         var cpCommand = new DockerCopyCommand(storage, "/beamable", srcPath, DockerCopyCommand.CopyType.HOST_TO_CONTAINER);
         var cpResult = await cpCommand.Start(null);
         if (!cpResult) return false;

         var restoreCommand = new MongoRestoreCommand(storage);
         return await restoreCommand.Start(null);
      }

      public static async Promise<bool> ClearMongoData(StorageObjectDescriptor storage)
      {
         Debug.Log("Clearing mongo");
         var storageCheck = new CheckImageReturnableCommand(storage);
         var isStorageRunning = await storageCheck.Start(null);
         if (isStorageRunning)
         {
            Debug.Log("stopping mongo");

            var stopComm = new StopImageCommand(storage);
            await stopComm.Start(null);
         }

         Debug.Log("deleting volumes");

         var deleteVolumes = new DeleteVolumeCommand(storage);
         var results = await deleteVolumes.Start(null);
         var err = results.Any(kvp => !kvp.Value);
         if (err)
         {
            Debug.LogError("Failed to remove all volumes");
            foreach (var kvp in results)
            {
               Debug.LogError($"{kvp.Key} -> {kvp.Value}");
            }
         }

         if (isStorageRunning)
         {
            Debug.Log("restarting mongo");

            var restart = new RunStorageCommand(storage);
            restart.Start();
         }

         return !err;
      }

      public static void GenerateClientSourceCode(MicroserviceDescriptor service)
      {
         var key = service.Name;
         Directory.CreateDirectory("Assets/Beamable/AutoGenerated/Microservices");
         var targetFile = $"Assets/Beamable/Autogenerated/Microservices/{service.Name}Client.cs";

         var tempFile = Path.Combine("Temp", $"{service.Name}Client.cs");

         var oldChecksum = Checksum(targetFile);

         var generator = new ClientCodeGenerator(service);
         generator.GenerateCSharpCode(tempFile);

         var nextChecksum = Checksum(tempFile);
         var requiresRebuild = !oldChecksum.Equals(nextChecksum);

//         Debug.Log($"Considering rebuilding {key}. {requiresRebuild} Old=[{oldChecksum}] Next=[{nextChecksum}]");
         if (requiresRebuild)
         {
            Debug.Log($"Generating client for {service.Name}");
            File.Copy(tempFile, targetFile, true);
         }
      }

      public static MicroserviceBuilder GetServiceBuilder(MicroserviceDescriptor descriptor)
      {
         var key = descriptor.Name;
         if (!_serviceToBuilder.ContainsKey(key))
         {
            var builder = new MicroserviceBuilder();
            builder.Init(descriptor);
            _serviceToBuilder.Add(key, builder);
         }
         return _serviceToBuilder[key];
      }

      public static MongoStorageBuilder GetStorageBuilder(StorageObjectDescriptor descriptor)
      {
         var key = descriptor.Name;

         if (_storageToBuilder.ContainsKey(key))
            return _storageToBuilder[key];

         var builder = new MongoStorageBuilder();
         builder.Init(descriptor);
         _storageToBuilder.Add(key, builder);
         return _storageToBuilder[key];
      }

      public static MicroserviceStateMachine GetServiceStateMachine(MicroserviceDescriptor descriptor)
      {
         var key = descriptor.Name;

         if (!_serviceToStateMachine.ContainsKey(key))
         {
            var pw = new CheckImageCommand(descriptor);
            pw.WriteLogToUnity = false;
            pw.Start();
            pw.Join();


            var initialState = pw.IsRunning ? MicroserviceState.RUNNING : MicroserviceState.IDLE;

            _serviceToStateMachine.Add(key, new MicroserviceStateMachine(descriptor, initialState));
         }

         return _serviceToStateMachine[key];
      }

      private static string Checksum(string filePath)
      {
         if (!File.Exists(filePath))
         {
            return "";
         }
         using(var stream = new BufferedStream(File.OpenRead(filePath), 1200000))
         {
            var md5 = MD5.Create();
            byte[] checksum = md5.ComputeHash(stream);
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
         }
      }

      public static event Action<ManifestModel, int> onBeforeDeploy;
      public static event Action<ManifestModel, int> onAfterDeploy;

      public static async Task Deploy(ManifestModel model, CommandRunnerWindow context, Action<IDescriptor> onServiceDeployed = null)
      {
         if (Descriptors.Count == 0) return; // don't do anything if there are no descriptors.

         var descriptorsCount = Descriptors.Count;
         onBeforeDeploy?.Invoke(model, descriptorsCount);

         // TODO perform sort of diff, and only do what is required. Because this is a lot of work.
         var de = await EditorAPI.Instance;

         var client = de.GetMicroserviceManager();
         var existingManifest = await client.GetCurrentManifest();
         var existingServiceToState = existingManifest.manifest.ToDictionary(s => s.serviceName);

         var nameToImageId = new Dictionary<string, string>();

         foreach (var descriptor in Descriptors)
         {
            Debug.Log($"Building service=[{descriptor.Name}]");
            var buildCommand = new BuildImageCommand(descriptor, false);
            await buildCommand.Start(context);

            var uploader = new ContainerUploadHarness(context);
            var msModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(descriptor);
            uploader.onProgress += msModel.OnDeployProgress;

            Debug.Log($"Getting Id service=[{descriptor.Name}]");
            var imageId = await uploader.GetImageId(descriptor);
            nameToImageId.Add(descriptor.Name, imageId);

            if (existingServiceToState.TryGetValue(descriptor.Name, out var existingReference))
            {
               if (existingReference.imageId == imageId)
               {
                  Debug.Log(string.Format(BeamableLogConstants.ContainerAlreadyUploadedMessage, descriptor.Name));
                  onServiceDeployed?.Invoke(descriptor);
                  continue;
               }
            }

            var entryModel = model.Services[descriptor.Name];
            var serviceDependencies = new List<ServiceDependency>();
            foreach (var storage in descriptor.GetStorageReferences())
            {
                serviceDependencies.Add(new ServiceDependency
                {
                    id = storage.Name,
                    type = "storage"
                });
            }
            entryModel.Dependencies = serviceDependencies;

            Debug.Log($"Uploading container service=[{descriptor.Name}]");

            await uploader.UploadContainer(descriptor, () =>
            {
                Debug.Log(string.Format(BeamableLogConstants.UploadedContainerMessage, descriptor.Name));
                onServiceDeployed?.Invoke(descriptor);
            },
            () =>
            {
                Debug.LogError(string.Format(BeamableLogConstants.CantUploadContainerMessage, descriptor.Name));
            }, imageId);
         }

         Debug.Log($"Deploying manifest");

         var manifest = model.Services.Select(kvp =>
         {
            kvp.Value.Enabled &= nameToImageId.TryGetValue(kvp.Value.Name, out var imageId);
            return new ServiceReference
            {
               serviceName = kvp.Value.Name,
               templateId = kvp.Value.TemplateId,
               enabled = kvp.Value.Enabled,
               comments = kvp.Value.Comment,
               imageId = imageId,
               dependencies = kvp.Value.Dependencies
            };
         }).ToList();

         var storages = model.Storages.Select(kvp =>
         {
             return new ServiceStorageReference
             {
                 storageName = kvp.Value.Name,
                 storageType = kvp.Value.Type,
                 templateId = kvp.Value.TemplateId,
                 enabled = kvp.Value.Enabled,
             };
         }).ToList();

         await client.Deploy(new ServiceManifest
         {
            comments = model.Comment,
            manifest = manifest,
            storages = storages
         });


         onAfterDeploy?.Invoke(model, descriptorsCount);

         Debug.Log("Service Deploy Complete");
      }

      public static void MicroserviceCreated(string serviceName)
      {
         var key = string.Format(SERVICE_PUBLISHED_KEY, serviceName);
         EditorPrefs.SetBool(key, false);
      }
   }
}
