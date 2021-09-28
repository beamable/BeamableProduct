using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Microservice.UI;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Model
{
   [System.Serializable]
   public class MicroservicesDataModel
   {
      private static MicroservicesDataModel _instance;
      private static bool _hasEnabledYet;

      public static MicroservicesDataModel Instance
      {
         get
         {
            var instance = GetInstance();
            if (!_hasEnabledYet)
            {
               instance.OnEnable();
               _hasEnabledYet = true;
            }

            return instance;
         }
         set
         {
            _instance = value;
            if (!_hasEnabledYet)
            {
               _instance.OnEnable();
               _hasEnabledYet = true;
            }
         }
      }

      public List<IBeamableService> AllServices = new List<IBeamableService>();

      private static MicroservicesDataModel GetInstance()
      {
         if (_instance != null)
         {
            return _instance;
         }
         _instance = new MicroservicesDataModel();
         _instance.RefreshLocal();
         _instance.RefreshServerManifest();
         return _instance;
      }

      public List<MicroserviceModel> Services = new List<MicroserviceModel>();
      public List<MongoStorageModel> Storages = new List<MongoStorageModel>();
      public ServiceManifest ServerManifest = new ServiceManifest();
      public GetStatusResponse Status = new GetStatusResponse();

      public Action<ServiceManifest> OnServerManifestUpdated;
      public Action<GetStatusResponse> OnStatusUpdated;

      public void RefreshLocal()
      {
         RefreshLocalServices();
         RefreshLocalStorages();
         AllServices = new List<IBeamableService>(Services.Count + Storages.Count);
         AllServices.AddRange(Services.Select(model => model as IBeamableService));
         AllServices.AddRange(Storages.Select(model => model as IBeamableService));
      }

      void RefreshLocalStorages()
      {
         var unseenStorages = new HashSet<MongoStorageModel>(Storages);
         
         foreach (var descriptor in Microservices.StorageDescriptors)
         {
            var existingService = GetModel<MongoStorageModel>(descriptor);
            if (existingService == null)
            {
               Storages.Add(MongoStorageModel.CreateNew(descriptor));
            }
            else
            {
               unseenStorages.Remove(existingService);
               existingService.Refresh(descriptor);
            }
         }

         Storages.RemoveAll(model => unseenStorages.Contains(model));
      }
      void RefreshLocalServices()
      {
         var unseenServices = new HashSet<MicroserviceModel>(Services);

         foreach (var descriptor in Microservices.Descriptors)
         {
            var existingService = GetModel<MicroserviceModel>(descriptor);
            if (existingService == null)
            {
               Services.Add(MicroserviceModel.CreateNew(descriptor,this));
            }
            else
            {
               unseenServices.Remove(existingService);
               existingService.Refresh(descriptor);
            }
         }
         Services.RemoveAll(model => unseenServices.Contains(model));
      }

      public void RefreshServerManifest()
      {
         EditorAPI.Instance.Then(b =>
         {
            b.GetMicroserviceManager().GetStatus().Then(status =>
            {
               Status = status;
               foreach (var serviceStatus in status.services)
               {
                  GetModel<MicroserviceModel>(serviceStatus.serviceName)?.EnrichWithStatus(serviceStatus);
               }
               OnStatusUpdated?.Invoke(status);
            });
            b.GetMicroserviceManager().GetCurrentManifest().Then(manifest =>
            {
               ServerManifest = manifest;
               foreach (var service in Services)
               {
                  var remoteService = manifest.manifest.FirstOrDefault(remote => string.Equals(remote.serviceName, service.Name));
                  service.EnrichWithRemoteReference(remoteService);
               }
               OnServerManifestUpdated?.Invoke(manifest);
            });
         });
      }

      public void AddLogMessage(IDescriptor descriptor, LogMessage message)
      {
         AllServices.FirstOrDefault(r => r.Equals(descriptor))?.GetLogs().AddMessage(message);
      }

      public ServiceStatus GetStatus(MicroserviceDescriptor descriptor)
      {
         return Status?.services?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
      }

      public ServiceReference GetReference(MicroserviceDescriptor descriptor)
      {
         return ServerManifest?.manifest?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
      }

      public T GetModel<T>(IDescriptor descriptor) where T : IBeamableService =>
         GetModel<T>(descriptor.Name);

      public T GetModel<T>(string serviceName) where T : IBeamableService
      {
         return (T)AllServices?.FirstOrDefault(s => s.GetDescriptor().Name.Equals(serviceName));
      }

      private void OnEnable()
      {
         Microservices.onAfterDeploy += MicroservicesOnonAfterDeploy;
         RefreshLocal();
         RefreshServerManifest();
      }

      private void MicroservicesOnonAfterDeploy(ManifestModel oldManifest, int serviceCount)
      {
         RefreshServerManifest();
      }

      public void Destroy()
      {
         Microservices.onAfterDeploy -= MicroservicesOnonAfterDeploy;

         _instance = null;
         _hasEnabledYet = false;

      }
   }
}