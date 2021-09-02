using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Model
{
   [System.Serializable]
   public class MicroservicesDataModel : ScriptableObject
   {
      private static MicroservicesDataModel _instance;
      public static bool HasInstance => _instance != null;
      public static MicroservicesDataModel Instance
      {
         get
         {
            if (_instance != null) return _instance;
            _instance = CreateInstance<MicroservicesDataModel>();
            _instance.RefreshLocalServices();
            _instance.RefreshServerManifest();
            return _instance;
         }
         set => _instance = value;
      }

      public List<MicroserviceModel> Services = new List<MicroserviceModel>();
      public ServiceManifest ServerManifest = new ServiceManifest();
      public GetStatusResponse Status = new GetStatusResponse();

      public Action<ServiceManifest> OnServerManifestUpdated;
      public Action<GetStatusResponse> OnStatusUpdated;

      public void RefreshLocalServices()
      {
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
                  Config = MicroserviceConfiguration.Instance.GetEntry(descriptor.Name)
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
               existingService.Config = MicroserviceConfiguration.Instance.GetEntry(descriptor.Name);
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
               foreach (var remoteService in manifest.manifest)
               {
                  GetModelForName(remoteService.serviceName)?.EnrichWithRemoteReference(remoteService);
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
         RefreshLocalServices();
      }

   }
}