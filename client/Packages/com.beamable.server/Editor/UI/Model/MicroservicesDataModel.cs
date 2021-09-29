using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Microservice.UI;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using JetBrains.Annotations;
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

      public List<IBeamableService> AllServices = new List<IBeamableService>();
      public List<MicroserviceModel> Services = new List<MicroserviceModel>();
      public List<MongoStorageModel> Storages = new List<MongoStorageModel>();
      public ServiceManifest ServerManifest = new ServiceManifest();
      public GetStatusResponse Status = new GetStatusResponse();

      public Action<ServiceManifest> OnServerManifestUpdated;
      public Action<GetStatusResponse> OnStatusUpdated;

      public void RefreshLocal()
      {
         AllServices = new List<IBeamableService>();
         RefreshLocalServices();
         RefreshLocalStorages();
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
         AllServices.AddRange(Services.Select(model => model as IBeamableService));
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
         AllServices.AddRange(Storages.Select(model => model as IBeamableService));
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

      public Dictionary<string, ServiceAvailability> GetAllServicesStatus()
      {
         var getServiceStatus = new Func<bool, bool, ServiceAvailability>((isLocally, isRemotely) =>
         {
            if (isLocally && isRemotely)
               return ServiceAvailability.LocalAndRemote;

            return isLocally ? ServiceAvailability.LocalOnly :
               isRemotely ? ServiceAvailability.RemoteOnly : ServiceAvailability.Unknown;
         });
         
         var result = new Dictionary<string, ServiceAvailability>();
         var servicesStatus = Status?.services;
         
         // foreach (var configEntry in MicroserviceConfiguration.Instance.StorageObjects)
         // {
         //    result.Add(configEntry.StorageName, getServiceStatus(ContainsModel(configEntry.StorageName), false));
         // }
         foreach (var configEntry in MicroserviceConfiguration.Instance.Microservices)
         {
            bool remotely = servicesStatus?.Find(status => status.serviceName.Equals(configEntry.ServiceName))!= null;
            result.Add(configEntry.ServiceName, getServiceStatus(ContainsModel(configEntry.ServiceName),remotely));
         }

         return result;
      }

      public ServiceReference GetReference(MicroserviceDescriptor descriptor)
      {
         return ServerManifest?.manifest?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
      }

      public ServiceType GetModelServiceType(string name)
      {
         var service = AllServices
            .FirstOrDefault(s => s.GetDescriptor().Name.Equals(name));
         return service?.GetServiceType() ?? ServiceType.MicroService;
      }

      public bool ContainsModel(string serviceName) => AllServices?.Any(s => s.GetDescriptor().Name.Equals(serviceName)) ?? false;

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

   public enum ServiceAvailability
   {
      LocalOnly,
      RemoteOnly,
      LocalAndRemote,
      Unknown
   }
}