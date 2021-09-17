using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI.Components;
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
        private VisualElement _microservicesListElement;

        private Dictionary<MicroserviceModel, MicroserviceVisualElement> _modelToVisual =
            new Dictionary<MicroserviceModel, MicroserviceVisualElement>();
        private CreateMicroserviceVisualElement _microserviceVisualElement;
        private MicroserviceActionPrompt _actionPrompt;

        public IEnumerable<MicroserviceVisualElement> MicroserviceVisualElements =>
            _microservicesListElement.Children().Where(ve => ve is MicroserviceVisualElement)
                .Cast<MicroserviceVisualElement>();

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
            _microservicesListElement = Root.Q<VisualElement>("listRoot");

            if (DockerCommand.DockerNotInstalled)
            {
                var dockerAnnouncement = new DockerAnnouncementModel();
                dockerAnnouncement.OnInstall = () => Application.OpenURL("https://docs.docker.com/get-docker/");
                var element = new DockerAnnouncementVisualElement() { DockerAnnouncementModel = dockerAnnouncement };
                Root.Q<VisualElement>("announcementList").Add(element);
                element.Refresh();
                return;
            }

            _microserviceVisualElement = new CreateMicroserviceVisualElement();
            _microservicesListElement.Add(_microserviceVisualElement);
            _microserviceVisualElement.OnCreateMicroserviceClicked += () => Root.SetEnabled(false);
            _modelToVisual.Clear();
            foreach (var service in Model.Services)
            {
                var serviceElement = new MicroserviceVisualElement {Model = service};
                _modelToVisual[service] = serviceElement;
                service.OnLogsDetached += () => { ServiceLogWindow.ShowService(service); };

                serviceElement.Refresh();
                service.OnSelectionChanged += b =>
                    OnAllServiceSelectedStatusChanged?.Invoke(Model.Services.All(m => m.IsSelected));

                service.OnSortChanged -= SortMicroservices;
                service.OnSortChanged += SortMicroservices;
                serviceElement.OnMicroserviceStartFailed = MicroserviceStartFailed;
                serviceElement.OnMicroserviceStopFailed = MicroserviceStopFailed;

                _microservicesListElement.Add(serviceElement);
            }

            _actionPrompt = _mainVisualElement.Q<MicroserviceActionPrompt>("actionPrompt");
            _actionPrompt.Refresh();
        }

        private void MicroserviceStartFailed()
        {
            _actionPrompt.SetVisible(Constants.PROMPT_STARTED_FAILURE, true, false);
        }

        private void MicroserviceStopFailed()
        {
            _actionPrompt.SetVisible(Constants.PROMPT_STOPPED_FAILURE, true, false);
        }

        public void DisplayCreatingNewService()
        {
            _microserviceVisualElement.Refresh();
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
        public void SortMicroservices() {
            var config = MicroserviceConfiguration.Instance;
            int Comparer(VisualElement a, VisualElement b) {
                if (a is CreateMicroserviceVisualElement) return -1;
                if (b is CreateMicroserviceVisualElement) return 1;
                var aName = ((MicroserviceVisualElement) a).Model.Name;
                var bName = ((MicroserviceVisualElement) b).Model.Name;
                return config.MicroserviceOrderComparer(aName, bName);
            }
            _microservicesListElement.Sort(Comparer);
        }
    }
}