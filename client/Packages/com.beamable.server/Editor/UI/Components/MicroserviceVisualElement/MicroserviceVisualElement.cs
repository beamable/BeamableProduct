using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class MicroserviceVisualElement : MicroserviceComponent
    {
        public MicroserviceVisualElement() : base(nameof(MicroserviceVisualElement))
        {
        }

        public new class UxmlFactory : UxmlFactory<MicroserviceVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        private const float _MIN_HEIGHT = 200.0f;
        private const float _MAX_HEIGHT = 500.0f;
        private const float _DETACHED_HEIGHT = 100.0f;
        private const float _DEFAULT_HEIGHT = 300.0f;

        private Button _buildDropDown;
        private Label _nameTextField;

        private Label _statusLabel;
        private VisualElement _statusIcon;
        private VisualElement _remoteStatusIcon;
        private Label _remoteStatusLabel;
        private Button _moreBtn;
        private BeamableCheckboxVisualElement _checkbox;
        private bool _mouseOverBuildDropdown;

        private Button _startButton;
        private VisualElement _logContainerElement;
        private Label _buildDefaultLabel;
        public MicroserviceModel Model { get; set; }

        private Action _defaultBuildAction;
        private LogVisualElement _logElement;

        private LoadingBarElement _loadingBar;
        private VisualElement _leftHeaderArea;
        private VisualElement _rightHeaderArea;
        private MicroserviceVisualElementSeparator _separator;
        private VisualElement _rootVisualElement;
        private float _storedHeight = 0;
        private VisualElement _header;

        public Action OnMicroserviceStartFailed { get; set; }
        public Action OnMicroserviceStopFailed { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Microservices.onBeforeDeploy -= SetupProgressBarForDeployment;

            if (Model == null) return;

            Model.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
            Model.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
            Model.OnBuild -= SetupProgressBarForBuild;
            Model.OnStart -= SetupProgressBarForStart;
            Model.OnStop -= SetupProgressBarForStop;
            Model.OnDockerLoginRequired -= LoginToDocker;
            Model.OnLogsAttachmentChanged -= CreateLogSection;
            Model.Builder.OnIsRunningChanged -= OnIsRunningChanged;
            Model.Builder.OnIsBuildingChanged -= OnIsBuildingChanged;
            Model.Builder.OnLastImageIdChanged -= HandleLastImageIdChanged;
        }

        public override void Refresh()
        {
            base.Refresh();
            name = Model.Name;
            _rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
            Root.Q<Button>("cancelBtn").RemoveFromHierarchy();

            _loadingBar = new LoadingBarElement();
            _loadingBar.Hidden = true;
            _loadingBar.Refresh();
            Root.Q("microserviceNewTitle")?.RemoveFromHierarchy();
            _rootVisualElement.Add(_loadingBar);
            _loadingBar.PlaceBehind(Root.Q("SubTitle"));

            Model.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
            Model.OnBuildAndStart += SetupProgressBarForBuildAndStart;
            Model.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
            Model.OnBuildAndRestart += SetupProgressBarForBuildAndRestart;
            Model.OnBuild -= SetupProgressBarForBuild;
            Model.OnBuild += SetupProgressBarForBuild;
            Model.OnStart -= SetupProgressBarForStart;
            Model.OnStart += SetupProgressBarForStart;
            Model.OnStop -= SetupProgressBarForStop;
            Model.OnStop += SetupProgressBarForStop;
            Model.OnDockerLoginRequired -= LoginToDocker;
            Model.OnDockerLoginRequired += LoginToDocker;
            Microservices.onBeforeDeploy -= SetupProgressBarForDeployment;
            Microservices.onBeforeDeploy += SetupProgressBarForDeployment;

            _nameTextField = Root.Q<Label>("microserviceTitle");
            _nameTextField.text = Model.Name;

            _buildDropDown = Root.Q<Button>("buildDropDown");
            var buildDropDownIcon = _buildDropDown.Q<Image>();
            _buildDefaultLabel = _buildDropDown.Q<Label>();
            buildDropDownIcon.RegisterCallback<MouseEnterEvent>(evt => _mouseOverBuildDropdown = true);
            buildDropDownIcon.RegisterCallback<MouseLeaveEvent>(evt => _mouseOverBuildDropdown = false);
            var buildBtnManipulator = new ContextualMenuManipulator(HandleBuildButtonClicked);
            buildBtnManipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            _buildDropDown.clickable.activators.Clear();
            _buildDropDown.AddManipulator(buildBtnManipulator);

            _startButton = Root.Q<Button>("start");
            _startButton.clickable.clicked += HandleStartButtonClicked;

            // _advanceDropDown = Root.Q<Button>("advanceBtn");
            // _advanceDropDown.clickable.clicked += () => { AdvanceDropDown_OnClicked(_advanceDropDown.worldBound); };

            _moreBtn = Root.Q<Button>("moreBtn");
            var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
            manipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            _moreBtn.clickable.activators.Clear();
            _moreBtn.AddManipulator(manipulator);
            _moreBtn.tooltip = "More...";

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.Refresh();
            _checkbox.SetWithoutNotify(Model.IsSelected);
            Model.OnSelectionChanged += _checkbox.SetWithoutNotify;
            _checkbox.OnValueChanged += b => Model.IsSelected = b;

            // create log element.
            _logContainerElement = Root.Q<VisualElement>("logContainer");
            Model.OnLogsAttachmentChanged -= CreateLogSection;
            Model.OnLogsAttachmentChanged += CreateLogSection;
            UpdateStartAndBuildButtons();

            Model.Builder.OnIsRunningChanged -= OnIsRunningChanged;
            Model.Builder.OnIsRunningChanged += OnIsRunningChanged;
            Model.Builder.OnIsBuildingChanged -= OnIsBuildingChanged;
            Model.Builder.OnIsBuildingChanged += OnIsBuildingChanged;

            Model.Builder.OnLastImageIdChanged -= HandleLastImageIdChanged;
            Model.Builder.OnLastImageIdChanged += HandleLastImageIdChanged;
            CreateLogSection(Model.AreLogsAttached);

            Model.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
            Model.OnRemoteReferenceEnriched += OnServiceReferenceChanged;

            _statusLabel = Root.Q<Label>("statusTitle");
            _remoteStatusLabel = Root.Q<Label>("remoteStatusTitle");

            _statusIcon = Root.Q<VisualElement>("statusIcon");

            UpdateStatusIcon();

            _remoteStatusIcon = Root.Q<VisualElement>("remoteStatusIcon");
            UpdateRemoteStatusIcon();

            _header = Root.Q("logHeader");
            _leftHeaderArea = Root.Q<VisualElement>("leftArea");
            _rightHeaderArea = Root.Q<VisualElement>("rightArea");
            UpdateHeaderColor();

            _separator = Root.Q<MicroserviceVisualElementSeparator>("separator");
            _separator.Setup(OnDrag);
            _separator.Refresh();
            UpdateModel();
        }

        async void UpdateModel()
        {
            await Model.Builder.CheckIfIsRunning();
        }

        private void OnDrag(float value)
        {
            if (!Model.AreLogsAttached)
            {
                return;
            }

            float layoutHeight = _rootVisualElement.layout.height;
            float newHeight = layoutHeight + value;

            newHeight = Mathf.Clamp(newHeight, _MIN_HEIGHT, _MAX_HEIGHT);
#if UNITY_2019_1_OR_NEWER
            _rootVisualElement.style.height = new StyleLength(newHeight);
#elif UNITY_2018
            _rootVisualElement.style.height = StyleValue<float>.Create(newHeight);
#endif
        }

        void LoginToDocker(Promise<Unit> onLogin)
        {
            DockerLoginVisualElement.ShowUtility().Then(onLogin.CompleteSuccess).Error(onLogin.CompleteError);
        }

        void HandleStartButtonClicked()
        {
            if (Model.IsRunning)
            {
                Model.Stop();
            }
            else
            {
                Model.Start();
            }
        }

        void HandleBuildClicked()
        {
            Model.Build();
        }

        private void OnIsRunningChanged(bool isRunning)
        {
            UpdateStartAndBuildButtons();
            UpdateStatusIcon();
            UpdateHeaderColor();
        }

        private void OnIsBuildingChanged(bool isBuilding)
        {
            UpdateStartAndBuildButtons();
            UpdateStatusIcon();
        }

        private void HandleLastImageIdChanged(string newId)
        {
            UpdateStartAndBuildButtons();
        }

        private void UpdateStartAndBuildButtons()
        {
            _startButton.text = Model.IsRunning ? Constants.STOP : Constants.START;
            _buildDefaultLabel.text = Constants.GetBuildButtonString(Model.IncludeDebugTools,
                Model.IsRunning ? Constants.BUILD_RESET : Constants.BUILD_START);

            if (Model.IsRunning)
            {
                _defaultBuildAction = () => Model.BuildAndRestart();
            }
            else
            {
                _defaultBuildAction = () => Model.BuildAndStart();
            }
            _startButton.SetEnabled(Model.Builder.HasImage && !Model.IsBuilding);
            _buildDropDown.SetEnabled(!Model.IsBuilding);
        }

        private void OnServiceReferenceChanged(ServiceReference serviceReference)
        {
            UpdateRemoteStatusIcon();
        }

        private void UpdateHeaderColor()
        {
            if (Model.IsRunning)
            {
                _header.AddToClassList("running");
            }
            else
            {
                _header.RemoveFromClassList("running");
            }
        }

        private void CreateLogSection(bool areLogsAttached)
        {
            _logElement?.Destroy();
            _logContainerElement.Clear();
            if (areLogsAttached)
            {
                CreateLogElement();

#if UNITY_2019_1_OR_NEWER
            _rootVisualElement.style.height = new StyleLength(_storedHeight > 0 ? _storedHeight : _DEFAULT_HEIGHT);
#elif UNITY_2018
                _rootVisualElement.style.height =
                    StyleValue<float>.Create(_storedHeight > 0 ? _storedHeight : _DEFAULT_HEIGHT);
#endif
                _storedHeight = 0;
            }
        }

        private void CreateLogElement()
        {
            _logElement = new LogVisualElement {Model = Model};
            _logElement.AddToClassList("logElement");
            _logElement.OnDetachLogs += OnLogsDetached;
            _logContainerElement.Add(_logElement);
            _logElement.Refresh();
        }

        private void OnLogsDetached()
        {
            _logElement.OnDetachLogs -= OnLogsDetached;
            _storedHeight = _rootVisualElement.layout.height;

#if UNITY_2019_1_OR_NEWER
            _rootVisualElement.style.height = new StyleLength(_DETACHED_HEIGHT);
#elif UNITY_2018
            _rootVisualElement.style.height =
                StyleValue<float>.Create(_DETACHED_HEIGHT);
#endif
        }

        private void UpdateRemoteStatusIcon()
        {
            _remoteStatusIcon.ClearClassList();
            string statusClassName;

            if (Model.RemoteReference?.enabled ?? false)
            {
                statusClassName = "remoteEnabled";
                _remoteStatusLabel.text = Constants.REMOTE_ENABLED;
            }
            else
            {
                statusClassName = "remoteDisabled";
                _remoteStatusLabel.text = Constants.REMOTE_NOT_ENABLED;
            }

            _remoteStatusIcon.tooltip = _remoteStatusLabel.text;
            _remoteStatusIcon.AddToClassList(statusClassName);
        }

        private void UpdateStatusIcon()
        {
            _statusIcon.ClearClassList();

            string statusClassName;
            string statusText;

            string status = Model.IsRunning ? "localRunning" :
                Model.IsBuilding ? "localBuilding" : "localStopped";
            switch (status)
            {
                case "localRunning":
                    statusText = "Local Running";
                    statusClassName = "localRunning";
                    break;
                case "localBuilding":
                    statusClassName = "localBuilding";
                    statusText = "Local Building";
                    break;
                case "localStopped":
                    statusClassName = "localStopped";
                    statusText = "Local Stopped";
                    break;
                default:
                    statusClassName = "different";
                    statusText = "Different";
                    break;
            }

            _statusIcon.tooltip = _statusLabel.text = statusText;
            _statusIcon.AddToClassList(statusClassName);
        }

        private void HandleBuildButtonClicked(ContextualMenuPopulateEvent evt)
        {
            if (_mouseOverBuildDropdown)
            {
                evt.menu.BeamableAppendAction("Build", pos => HandleBuildClicked());
                evt.menu.BeamableAppendAction(Model.IncludeDebugTools
                    ? Constants.BUILD_DISABLE_DEBUG
                    : Constants.BUILD_ENABLE_DEBUG, pos =>
                {
                    Model.IncludeDebugTools = !Model.IncludeDebugTools;
                    UpdateStartAndBuildButtons();
                });
            }
            else
            {
                _defaultBuildAction?.Invoke();
            }
        }

        private void SetupProgressBarForBuildAndStart(Task task)
        {
            var _ = new GroupLoadingBarUpdater("Build and Run", _loadingBar, false,
                new StepLogParser(new VirtualLoadingBar(), Model, null),
                new RunImageLogParser(new VirtualLoadingBar(), Model)
            );
        }

        private void SetupProgressBarForBuildAndRestart(Task task)
        {
            var _ = new GroupLoadingBarUpdater("Build and Rerun", _loadingBar, false,
                new StepLogParser(new VirtualLoadingBar(), Model, null),
                new RunImageLogParser(new VirtualLoadingBar(), Model),
                new StopImageLogParser(new VirtualLoadingBar(), Model)
            );
        }

        private void SetupProgressBarForBuild(Task task)
        {
            new StepLogParser(_loadingBar, Model, task);
        }

        private void SetupProgressBarForStart(Task task)
        {
            // We have two ways. Either store reference or return instance as event parameter
            new RunImageLogParser(_loadingBar, Model)
            {
                OnFailure = OnStartFailed
            };
        }

        private void OnStartFailed()
        {
            OnMicroserviceStartFailed?.Invoke();
        }

        private void SetupProgressBarForStop(Task task)
        {
            new StopImageLogParser(_loadingBar, Model)
            {
                OnFailure = OnStopFailed
            };
        }

        private void OnStopFailed()
        {
            OnMicroserviceStopFailed?.Invoke();
        }

        private void SetupProgressBarForDeployment(ManifestModel _, int __)
        {
            var ___ = new GroupLoadingBarUpdater("Build and Deploy", _loadingBar, false,
                new StepLogParser(new VirtualLoadingBar(), Model, null),
                new DeployMSLogParser(new VirtualLoadingBar(), Model)
            );
        }
    }
}