using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;

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
      public List<MicroserviceModel> Services => AllServices.Where(service => service.ServiceType == ServiceType.MicroService).Select(service => service as MicroserviceModel).ToList();
      public List<MongoStorageModel> Storages => AllServices.Where(service => service.ServiceType == ServiceType.StorageObject).Select(service => service as MongoStorageModel).ToList();
      public ServiceManifest ServerManifest = new ServiceManifest();
      public GetStatusResponse Status = new GetStatusResponse();
      public ServicesDisplayFilter Filter = ServicesDisplayFilter.AllTypes;

      public Action<ServiceManifest> OnServerManifestUpdated;
      public Action<GetStatusResponse> OnStatusUpdated;

      public void RefreshLocal()
      {
         var unseen = new HashSet<IBeamableService>(AllServices);
         foreach (var descriptor in Microservices.AllDescriptors)
         {
            var serviceExists = ContainsModel(descriptor.Name);
            if (serviceExists)
            {
               var service = GetModel<IBeamableService>(descriptor.Name);
               unseen.Remove(GetModel<IBeamableService>(descriptor.Name));
               service.Refresh(descriptor);
               continue;
            }

            IBeamableService newService;
            if (descriptor.ServiceType == ServiceType.StorageObject)
            {
               newService = MongoStorageModel.CreateNew(descriptor as StorageObjectDescriptor);
            }
            else
            {
               newService = MicroserviceModel.CreateNew(descriptor as MicroserviceDescriptor, this);
            }
            AllServices.Add(newService);
         }

         AllServices.RemoveAll(model => unseen.Contains(model));
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
         AllServices.FirstOrDefault(r => r.Descriptor.Name.Equals(descriptor.Name))
            ?.Logs.AddMessage(message);
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

         foreach (var configEntry in MicroserviceConfiguration.Instance.Microservices)
         {
            var remotely = servicesStatus?.Find(status => status.serviceName.Equals(configEntry.ServiceName))!= null;
            result.Add(configEntry.ServiceName, getServiceStatus(ContainsModel(configEntry.ServiceName), remotely));
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
            .FirstOrDefault(s => s.Descriptor.Name.Equals(name));
         return service?.ServiceType ?? ServiceType.MicroService;
      }

      public bool ContainsModel(string serviceName) => AllServices?.Any(s => s.Descriptor.Name.Equals(serviceName)) ?? false;

      public T GetModel<T>(IDescriptor descriptor) where T : IBeamableService =>
         GetModel<T>(descriptor.Name);

      public T GetModel<T>(string serviceName) where T : IBeamableService
      {
         return (T)AllServices?.FirstOrDefault(s => s.Descriptor.Name.Equals(serviceName));
      }

      public MicroserviceModel GetMicroserviceModel(IDescriptor descriptor) => GetModel<MicroserviceModel>(descriptor);

      public MongoStorageModel GetStorageModel(IDescriptor descriptor) => GetModel<MongoStorageModel>(descriptor);

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

   public enum ServicesDisplayFilter
   {
      AllTypes,
      Microservices,
      Storages
   }
}