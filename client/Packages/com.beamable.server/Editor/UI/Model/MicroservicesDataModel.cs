using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.Model
{
	[System.Serializable]
	public class MicroservicesDataModel : ISerializationCallbackReceiver
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

		[SerializeField] private List<MicroserviceModel> _localMicroserviceModels;
		[SerializeField] private List<MongoStorageModel> _localStorageModels;

		public List<IBeamableService> AllLocalServices = new List<IBeamableService>();
		public List<IBeamableService> AllRemoteOnlyServices = new List<IBeamableService>();
		public List<MicroserviceModel> Services => AllLocalServices.Where(service => service.ServiceType == ServiceType.MicroService).Select(service => service as MicroserviceModel).ToList();
		public List<MongoStorageModel> Storages => AllLocalServices.Where(service => service.ServiceType == ServiceType.StorageObject).Select(service => service as MongoStorageModel).ToList();
		public ServiceManifest ServerManifest = new ServiceManifest();
		public GetStatusResponse Status = new GetStatusResponse();
		public ServicesDisplayFilter Filter = ServicesDisplayFilter.AllTypes;

		public Action<ServiceManifest> OnServerManifestUpdated;
		public Action<GetStatusResponse> OnStatusUpdated;

		public void RefreshLocal()
		{
			var unseen = new HashSet<IBeamableService>(AllLocalServices);
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
				AllLocalServices.Add(newService);
			}

			AllLocalServices.RemoveAll(model => unseen.Contains(model));
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

				 foreach (var singleManifest in ServerManifest.manifest)
				 {
					 if (ContainsRemoteOnlyModel(singleManifest.serviceName))
						 continue;

					 var descriptor = new MicroserviceDescriptor
					 {
						 Name = singleManifest.serviceName
					 };

					 AllRemoteOnlyServices.Add(RemoteMicroserviceModel.CreateNew(descriptor, this));
				 }

				 OnServerManifestUpdated?.Invoke(manifest);
			 });
			});
		}

		public void AddLogMessage(IDescriptor descriptor, LogMessage message)
		{
			AllLocalServices.FirstOrDefault(r => r.Descriptor.Name.Equals(descriptor.Name))
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
				var name = configEntry.ServiceName;
				var remotely = servicesStatus?.Find(status => status.serviceName.Equals(name)) != null;
				if (!result.ContainsKey(name))
					result.Add(name, getServiceStatus(ContainsModel(configEntry.ServiceName), remotely));
			}

			foreach (var storage in MicroserviceConfiguration.Instance.StorageObjects)
			{
				var name = storage.StorageName;
				var remotely = servicesStatus?.Find(status => status.serviceName.Equals(name)) != null;
				if (!result.ContainsKey(name))
					result.Add(name, getServiceStatus(ContainsModel(name), remotely));
			}

			return result;
		}

		public ServiceReference GetReference(MicroserviceDescriptor descriptor)
		{
			return ServerManifest?.manifest?.FirstOrDefault(r => r.serviceName.Equals(descriptor.Name));
		}

		public ServiceType GetModelServiceType(string name)
		{
			var allServices = new List<IBeamableService>();
			allServices.AddRange(AllLocalServices);
			allServices.AddRange(AllRemoteOnlyServices);

			var service = allServices
			   .FirstOrDefault(s => s.Descriptor.Name.Equals(name));
			return service?.ServiceType ?? ServiceType.MicroService;
		}

		public bool ContainsRemoteOnlyModel(string serviceName) => AllRemoteOnlyServices?.Any(s => s.Descriptor.Name.Equals(serviceName)) ?? false;
		public bool ContainsModel(string serviceName) => AllLocalServices?.Any(s => s.Descriptor.Name.Equals(serviceName)) ?? false;

		public T GetModel<T>(IDescriptor descriptor) where T : IBeamableService =>
		   GetModel<T>(descriptor.Name);

		public T GetModel<T>(string serviceName) where T : IBeamableService
		{
			var allServices = new List<IBeamableService>();
			allServices.AddRange(AllLocalServices);
			allServices.AddRange(AllRemoteOnlyServices);

			return (T)allServices?.FirstOrDefault(s => s is T && s.Descriptor.Name.Equals(serviceName));
		}

		public MicroserviceModel GetMicroserviceModel(IDescriptor descriptor) => GetModel<MicroserviceModel>(descriptor);

		public MongoStorageModel GetStorageModel(IDescriptor descriptor) => GetModel<MongoStorageModel>(descriptor);

		private void OnEnable()
		{
			Microservices.OnDeploySuccess += HandleMicroservicesDeploySuccess;
			RefreshLocal();
			RefreshServerManifest();
		}

		private void HandleMicroservicesDeploySuccess(ManifestModel oldManifest, int serviceCount)
		{
			RefreshServerManifest();
		}

		public void Destroy()
		{
			Microservices.OnDeploySuccess -= HandleMicroservicesDeploySuccess;

			_instance = null;
			_hasEnabledYet = false;

		}

		public void OnBeforeSerialize()
		{
			_localMicroserviceModels = new List<MicroserviceModel>();
			_localStorageModels = new List<MongoStorageModel>();
			foreach (var service in AllLocalServices)
			{
				switch (service)
				{
					case MicroserviceModel microserviceModel:
						_localMicroserviceModels.Add(microserviceModel);
						break;
					case MongoStorageModel mongoModel:
						_localStorageModels.Add(mongoModel);
						break;
				}
			}
		}

		public void OnAfterDeserialize()
		{
			void AddModels<T>(List<T> models, List<IBeamableService> listToPopulate) where T : ServiceModelBase
			{
				foreach (var service in models.ToArray())
				{
					IBeamableService existing = null;

					for (int i = 0; i < listToPopulate.Count; i++)
					{
						if (string.Equals(listToPopulate[i]?.Descriptor?.Name, service?.Descriptor?.Name))
						{
							existing = listToPopulate[i];
							break;
						}
					}

					if (existing == null)
					{
						// Types aren't serialized properly so we store their assembly qualified name and retrieve it afterwards.
						switch (service)
						{
							case MicroserviceModel microserviceModel:
								if (!string.IsNullOrEmpty(microserviceModel.AssemblyQualifiedMicroserviceTypeName))
									microserviceModel.Descriptor.Type = Type.GetType(microserviceModel.AssemblyQualifiedMicroserviceTypeName);
								if (microserviceModel.Builder != null)
									((MicroserviceBuilder)microserviceModel.Builder).Descriptor = microserviceModel.Descriptor;
								break;
							case MongoStorageModel mongoModel:
								_localStorageModels.Add(mongoModel);
								break;
						}

						listToPopulate.Add(service);

					}
				}
			}
			AddModels(_localMicroserviceModels, AllLocalServices);
			AddModels(_localStorageModels, AllLocalServices);
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
