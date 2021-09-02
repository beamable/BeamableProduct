using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.DockerCommands;
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
        private CreateMicroserviceVisualElement _microserviceVisualElement;

        public IEnumerable<MicroserviceVisualElement> MicroserviceVisualElements =>
            _microservicesListElement.Children().Where(ve => ve is MicroserviceVisualElement).Cast<MicroserviceVisualElement>();

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
                var installDockerInfo = new AnnouncementModel
                {
                    Status = ToolboxAnnouncementStatus.INFO,
                    ActionText = "Install Docker",
                    CustomIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        "Packages/com.beamable/Editor/UI/Common/Icons/welcome.png"),
                    Action = () => Application.OpenURL("https://docs.docker.com/get-docker/"),
                    TitleElement = new Label("Docker not installed"),
                    DescriptionElement = new Label("Docker was not detected. Make sure it is available on your System Path.")
                };
                var element = new AnnouncementVisualElement {AnnouncementModel = installDockerInfo};
                Root.Q<VisualElement>("announcementList").Add(element);
                element.Refresh();
                return;
            }
            
            _microserviceVisualElement  = new CreateMicroserviceVisualElement();
            _microservicesListElement.Add(_microserviceVisualElement);
            _microserviceVisualElement.OnCreateMicroserviceClicked += () => Root.SetEnabled(false);

            foreach (var service in Model.Services)
            {
                var serviceElement = new MicroserviceVisualElement {Model = service};

                service.OnLogsDetached += () =>
                {
                    ServiceLogWindow.ShowService(service);
                };

                serviceElement.Refresh();
                service.OnSelectionChanged += b =>
                    OnAllServiceSelectedStatusChanged?.Invoke(Model.Services.All(m => m.IsSelected));
                
                _microservicesListElement.Add(serviceElement);
            }

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
            var loadingBarUpdater = new MergedBarUpdater(loadingBar, "Building Microservices", true);
            foreach (var microservice in Model.Services)
            {
                if(!microservice.IsSelected)
                    continue;
                if(microservice.IsRunning) {
                    var task = microservice.BuildAndRestart();
                    var mergedParser = new MergedBarUpdater(loadingBarUpdater.CreateDummyLoadingBar(), "Build and Run");
                    new StepLogParser(mergedParser.CreateDummyLoadingBar(), microservice, task);
                    new RunImageLogParser(mergedParser.CreateDummyLoadingBar(), microservice);
                }
                else {
                    var task = microservice.Build();
                    new StepLogParser(loadingBarUpdater.CreateDummyLoadingBar(), microservice, task);
                }
            }
        }

        public void BuildAndStartAllMicroservices(ILoadingBar loadingBar)
        {
            var loadingBarUpdater = new MergedBarUpdater(loadingBar, "Starting Microservices", true);
            foreach (var microservice in Model.Services)
            {
                if(!microservice.IsSelected)
                    continue;
                
                Task task;
                if (microservice.IsRunning) 
                    task = microservice.BuildAndRestart();
                else
                    task = microservice.BuildAndStart();
                var mergedParser = new MergedBarUpdater(loadingBarUpdater.CreateDummyLoadingBar(), "Build and Run");
                new StepLogParser(mergedParser.CreateDummyLoadingBar(), microservice, task);
                new RunImageLogParser(mergedParser.CreateDummyLoadingBar(), microservice);
            }
        }

    }
}