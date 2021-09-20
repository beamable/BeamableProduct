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

      private static MicroservicesDataModel GetInstance()
      {
         if (_instance != null)
         {
            return _instance;
         }
         _instance = new MicroservicesDataModel();
         _instance.RefreshLocalServices();
         _instance.RefreshServerManifest();
         return _instance;
      }

      public List<MicroserviceModel> Services = new List<MicroserviceModel>();
      public ServiceManifest ServerManifest = new ServiceManifest();
      public GetStatusResponse Status = new GetStatusResponse();

      public Action<ServiceManifest> OnServerManifestUpdated;
      public Action<GetStatusResponse> OnStatusUpdated;

      public void RefreshLocalServices() {
         var config = MicroserviceConfiguration.Instance;
         var unseenServices = new HashSet<MicroserviceModel>(Services);

         foreach (var descriptor in Microservices.Descriptors)
         {
            var existingService = GetModelForDescriptor(descriptor);
            if (existingService == null)
            {
               Services.Add(new MicroserviceModel
               {
                  Descriptor = descriptor,
                  Builder =  Microservices.GetServiceBuilder(descriptor),
                  Logs = new LogMessageStore(),
                  RemoteReference = GetReference(descriptor),
                  RemoteStatus = GetStatus(descriptor),
                  Config = config.GetEntry(descriptor.Name)
               });
            }
            else
            {
               unseenServices.Remove(existingService);
               // reset the descriptor and statemachines; because they aren't system.serializable durable.
               existingService.Descriptor = descriptor;
               var oldBuilder = existingService.Builder;
               existingService.Builder = Microservices.GetServiceBuilder(descriptor);
               existingService.Builder.ForwardEventsTo(oldBuilder);
               existingService.Config = config.GetEntry(descriptor.Name);
            }
         }

         foreach (var unseenService in unseenServices)
         {
            Services.Remove(unseenService);
         }
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
                  GetModelForName(serviceStatus.serviceName)?.EnrichWithStatus(serviceStatus);
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

      public void AddLogMessage(MicroserviceDescriptor descriptor, LogMessage message)
      {
         GetModelForDescriptor(descriptor).Logs.AddMessage(message);
      }

      public ServiceStatus GetStatus(MicroserviceDescriptor descriptor)
      {
         return Status?.services?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
      }

      public ServiceReference GetReference(MicroserviceDescriptor descriptor)
      {
         return ServerManifest?.manifest?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
      }

      public MicroserviceModel GetModelForDescriptor(MicroserviceDescriptor descriptor) =>
         GetModelForName(descriptor.Name);

      public MicroserviceModel GetModelForName(string serviceName)
      {
         return Services?.FirstOrDefault(s => s.Descriptor.Name.Equals(serviceName));
      }

      private void OnEnable()
      {
         Microservices.onAfterDeploy += MicroservicesOnonAfterDeploy;
         RefreshLocalServices();
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