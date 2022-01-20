using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI.Components;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class MicroserviceContentVisualElement : MicroserviceComponent
    {
        public event Action<bool> OnAllServiceSelectedStatusChanged;

        private VisualElement _mainVisualElement;
        private ListView _listView;
        private ScrollView _scrollView;
        private VisualElement _servicesListElement;

        private readonly Dictionary<ServiceModelBase, ServiceBaseVisualElement> _modelToVisual = new Dictionary<ServiceModelBase, ServiceBaseVisualElement>();
        private Dictionary<ServiceType, CreateServiceBaseVisualElement> _servicesCreateElements;
        private MicroserviceActionPrompt _actionPrompt;
        private bool _dockerHubIsRunning;

        public IEnumerable<ServiceBaseVisualElement> ServiceVisualElements =>
            _servicesListElement.Children().Where(ve => ve is ServiceBaseVisualElement)
                .Cast<ServiceBaseVisualElement>();

        public new class UxmlFactory : UxmlFactory<MicroserviceContentVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as MicroserviceContentVisualElement;
            }
        }

        public MicroserviceContentVisualElement() : base(nameof(MicroserviceContentVisualElement))
        {
        }

        public MicroservicesDataModel Model { get; set; }

        public override void Refresh()
        {
	        base.Refresh();

            _mainVisualElement = Root.Q<VisualElement>("mainVisualElement");
            _scrollView = Root.Q<ScrollView>();
            _servicesListElement = Root.Q<VisualElement>("listRoot");
            _servicesCreateElements = new Dictionary<ServiceType, CreateServiceBaseVisualElement>();
            _dockerHubIsRunning = !MicroserviceConfiguration.Instance.DockerAppCheckInMicroservicesWindow
                                  || IsDockerAppRunning();

            if (DockerCommand.DockerNotInstalled || !_dockerHubIsRunning)
            {
	            ShowDockerNotInstalledAnnouncement();
            }
            if(DockerCommand.DockerNotInstalled)
	            return;

            CreateNewServiceElement(ServiceType.MicroService, new CreateMicroserviceVisualElement());
            CreateNewServiceElement(ServiceType.StorageObject, new CreateStorageObjectVisualElement());

            _modelToVisual.Clear();

            SetupServicesStatus();

            _actionPrompt = _mainVisualElement.Q<MicroserviceActionPrompt>("actionPrompt");
            _actionPrompt.Refresh();
        }

        private MicroserviceVisualElement GetMicroserviceVisualElement(string serviceName)
        {
            var service = Model.GetModel<MicroserviceModel>(serviceName);
            if (service == null)
            {
	            return null;
            }

            var serviceElement = new MicroserviceVisualElement { Model = service };
            _modelToVisual[service] = serviceElement;
            service.OnLogsDetached += () => { ServiceLogWindow.ShowService(service); };

            serviceElement.Refresh();
            service.OnSelectionChanged += b =>
	            OnAllServiceSelectedStatusChanged?.Invoke(Model.Services.All(m => m.IsSelected));

            service.OnSortChanged -= SortMicroservices;
            service.OnSortChanged += SortMicroservices;
            serviceElement.OnServiceStartFailed = MicroserviceStartFailed;
            serviceElement.OnServiceStopFailed = MicroserviceStopFailed;

            return serviceElement;
        }

        private RemoteMicroserviceVisualElement GetRemoteMicroserviceVisualElement(string serviceName)
        {
            var service = Model.GetModel<RemoteMicroserviceModel>(serviceName);

            if (service != null)
            {
                var serviceElement = new RemoteMicroserviceVisualElement { Model = service };

                _modelToVisual[service] = serviceElement;
                serviceElement.Refresh();

                service.OnSortChanged -= SortMicroservices;
                service.OnSortChanged += SortMicroservices;

                return serviceElement;
            }

            return null;
        }

        private StorageObjectVisualElement GetStorageObjectVisualElement(string serviceName)
        {
            var mongoService = Model.GetModel<MongoStorageModel>(serviceName);

            if (mongoService != null)
            {
                var mongoServiceElement = new StorageObjectVisualElement { Model = mongoService };
                _modelToVisual[mongoService] = mongoServiceElement;
                mongoService.OnLogsDetached += () => { ServiceLogWindow.ShowService(mongoService); };

                mongoServiceElement.Refresh();
                mongoService.OnSelectionChanged += b =>
                    OnAllServiceSelectedStatusChanged?.Invoke(Model.Storages.All(m => m.IsSelected));

                mongoService.OnSortChanged -= SortStorages;
                mongoService.OnSortChanged += SortStorages;

                return mongoServiceElement;

            }

            return null;
        }

        private StorageObjectVisualElement GetRemoteStorageObjectVisualElement(string serviceName)
        {
            var mongoService = Model.GetModel<MongoStorageModel>(serviceName);

            if (mongoService != null)
            {
                var mongoServiceElement = new RemoteStorageObjectVisualElement { Model = mongoService };
                _modelToVisual[mongoService] = mongoServiceElement;
                mongoService.OnLogsDetached += () => { ServiceLogWindow.ShowService(mongoService); };

                mongoServiceElement.Refresh();
                mongoService.OnSelectionChanged += b =>
                    OnAllServiceSelectedStatusChanged?.Invoke(Model.Storages.All(m => m.IsSelected));

                mongoService.OnSortChanged -= SortStorages;
                mongoService.OnSortChanged += SortStorages;

                return mongoServiceElement;

            }

            return null;
        }

		private void MicroserviceStartFailed()
        {
            _actionPrompt.SetVisible(Constants.PROMPT_STARTED_FAILURE, true, false);
        }

        private void MicroserviceStopFailed()
        {
            _actionPrompt.SetVisible(Constants.PROMPT_STOPPED_FAILURE, true, false);
        }

        public void DisplayCreatingNewService(ServiceType serviceType)
        {
            _servicesCreateElements[serviceType].Refresh();
            EditorApplication.delayCall += () => _scrollView.verticalScroller.value = 0f;
        }

        public void SetAllMicroserviceSelectedStatus(bool selected)
        {
            foreach (var microservice in Model.Services)
            {
                microservice.IsSelected = selected;
            }
        }

        public void BuildAllMicroservices(ILoadingBar loadingBar)
        {
            var children = new List<LoadingBarUpdater>();

            foreach (var microservice in Model.Services)
            {
                if (!microservice.IsSelected)
                    continue;
                if (microservice.IsRunning)
                    microservice.BuildAndRestart();
                else
                    microservice.Build();

                var element = _modelToVisual[microservice];
                var subLoader = element.Q<LoadingBarElement>();
                children.Add(subLoader.Updater);
            }

            var _ = new GroupLoadingBarUpdater("Building Microservices", loadingBar, false, children.ToArray());
        }

        public void BuildAndStartAllMicroservices(ILoadingBar loadingBar)
        {
            var children = new List<LoadingBarUpdater>();
            foreach (var microservice in Model.Services)
            {
                if (!microservice.IsSelected)
                    continue;

                if (microservice.IsRunning)
                    microservice.BuildAndRestart();
                else
                    microservice.BuildAndStart();

                var element = _modelToVisual[microservice];
                var subLoader = element.Q<LoadingBarElement>();
                children.Add(subLoader.Updater);
            }

            var _ = new GroupLoadingBarUpdater("Starting Microservices", loadingBar, false, children.ToArray());
        }

        public void SortServices(ServiceType serviceType)
        {
            var config = MicroserviceConfiguration.Instance;

            int Comparer(VisualElement a, VisualElement b)
            {
                if (a is CreateServiceBaseVisualElement) return -1;
                if (b is CreateServiceBaseVisualElement) return 1;

				switch (serviceType)
				{
					case ServiceType.MicroService:
						if (b is StorageObjectVisualElement)
							return -1;
						break;
					case ServiceType.StorageObject:
						if (b is MicroserviceVisualElement)
							return 1;
						break;
					default:
						break;
				}

                return config.OrderComparer(a.name, b.name, serviceType);
            }

			_servicesListElement.Sort(Comparer);
		}

		public void SortMicroservices()
		{
			SortServices(ServiceType.MicroService);
		}

		public void SortStorages()
		{
			SortServices(ServiceType.StorageObject);
		}

		private bool ShouldDisplayService(ServiceType type)
        {
	        switch (Model.Filter)
	        {
		        case ServicesDisplayFilter.AllTypes:
			        return true;
		        case ServicesDisplayFilter.Microservices:
			        return type == ServiceType.MicroService;
		        case ServicesDisplayFilter.Storages:
			        return type == ServiceType.StorageObject;
		        default:
			        return false;
	        }
        }

        private void ShowDockerNotInstalledAnnouncement()
        {
	        var dockerAnnouncement = new DockerAnnouncementModel();
	        dockerAnnouncement.IsDockerInstalled = !DockerCommand.DockerNotInstalled;
	        if (DockerCommand.DockerNotInstalled)
	        {
		        dockerAnnouncement.OnInstall = () => Application.OpenURL("https://docs.docker.com/get-docker/");
	        }
	        else
	        {
		        dockerAnnouncement.OnInstall = Refresh;
	        }
	        var element = new DockerAnnouncementVisualElement() { DockerAnnouncementModel = dockerAnnouncement };
	        Root.Q<VisualElement>("announcementList").Add(element);
	        element.Refresh();
        }

        private void CreateNewServiceElement(ServiceType serviceType, CreateServiceBaseVisualElement service)
        {
	        service.OnCreateServiceClicked += () => Root.SetEnabled(false);
	        _servicesCreateElements.Add(serviceType, service);
	        _servicesListElement.Add(service);
        }

        private void SetupServicesStatus()
        {
	        var hasStorageDependency = false;
	        foreach (var serviceStatus in Model.GetAllServicesStatus())
	        {
		        if (serviceStatus.Value == ServiceAvailability.Unknown)
			        continue;

		        var serviceType = Model.GetModelServiceType(serviceStatus.Key);
		        if (!ShouldDisplayService(serviceType))
			        continue;

		        ServiceBaseVisualElement serviceElement = null;

		        switch (serviceType)
		        {
			        case ServiceType.MicroService:

				        var val = false;
				        if (serviceStatus.Value != ServiceAvailability.RemoteOnly)
					        serviceElement = GetMicroserviceVisualElement(serviceStatus.Key);
				        else
					        serviceElement = GetRemoteMicroserviceVisualElement(serviceStatus.Key);

				        hasStorageDependency |= val;
				        break;
			        case ServiceType.StorageObject:
				        if (serviceStatus.Value != ServiceAvailability.RemoteOnly)
					        serviceElement = GetStorageObjectVisualElement(serviceStatus.Key);
				        else
					        serviceElement = GetRemoteStorageObjectVisualElement(serviceStatus.Key);
				        break;
			        default:
				        throw new ArgumentOutOfRangeException();
		        }

		        if (serviceElement != null)
		        {
			        serviceElement.SetEnabled(_dockerHubIsRunning);
			        _servicesListElement.Add(serviceElement);
		        }
	        }
        }

        static bool IsDockerAppRunning()
        {
	        var procList = Process.GetProcesses();
	        for (int i = 0; i < procList.Length; i++)
	        {
		        try
		        {
#if UNITY_EDITOR_WIN
			        const string procName = "docker desktop";
#else
			        const string procName = "docker";
#endif
			        if (procList[i].ProcessName.ToLower().Contains(procName))
			        {
				        return true;
			        }
		        }
		        catch { }
	        }

	        return false;
        }
    }
}
