
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI.Components;
using Editor.UI.Components.DockerLoginWindow;
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
  public class MicroserviceVisualElement : MicroserviceComponent
    {
        private Button _buildDropDown;
        private Button _advanceDropDown;


        private VisualElement _logListRoot;
        private ListView _listView;


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

        private Label _nameTextField;
        private string _nameBackup;
        // private List<LogMessageModel> testLogList;
        private Label _statusLabel;
        private VisualElement _statusIcon;
        private VisualElement _remoteStatusIcon;
        private Label _remoteStatusLabel;
        private Button _popupBtn;
        private Button _moreBtn;
        private BeamableCheckboxVisualElement _checkbox;
        private bool _mouseOverBuildDropdown;

        private object _logVisualElement;
        private Button _startButton;
        private VisualElement _logContainerElement;
        private Label _buildDefaultLabel;
        public MicroserviceModel Model { get; set; }

        private Action _defaultBuildAction;
        private LogVisualElement _logElement;

        private LoadingBarElement _loadingBar;

        public override void Refresh()
        {
            base.Refresh();

            Root.Q<Button>("cancelBtn").RemoveFromHierarchy();

            _loadingBar = new LoadingBarElement();
            _loadingBar.Hidden = true;
            _loadingBar.Refresh();
            Root.Q("mainVisualElement").Add(_loadingBar);
            Root.Q("microserviceNewTitle")?.RemoveFromHierarchy();
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
            CreateLogSection(Model.AreLogsAttached);

            _statusLabel = Root.Q<Label>("statusTitle");
            _remoteStatusLabel = Root.Q<Label>("remoteStatusTitle");

            _statusIcon = Root.Q<VisualElement>("statusIcon");

            UpdateStatusIcon();

            _remoteStatusIcon = Root.Q<VisualElement>("remoteStatusIcon");
            UpdateRemoteStatusIcon();
        }

        void LoginToDocker(Promise<Unit> onLogin)
        {
            DockerLoginVisualElement.ShowUtility().Then(onLogin.CompleteSuccess).Error(onLogin.CompleteError);
        }

        void HandleStartButtonClicked()
        {
            DisableBuildAndStart();
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
            DisableBuildAndStart();
            Model.Build();
        }

        private void OnBuildingChanged(bool isBuilding)
        {
            _buildDropDown.SetEnabled(!isBuilding);
        }

        private void OnIsRunningChanged(bool isRunning)
        {
            UpdateStartAndBuildButtons();
            UpdateStatusIcon();
        }

        private void OnIsBuildingChanged(bool isBuilding)
        {
            UpdateStartAndBuildButtons();
            UpdateStatusIcon();
        }

        private void UpdateStartAndBuildButtons()
        {
            if (Model.IsRunning)
            {
                SetAsRunningService();
            }
            else
            {
                SetAsStoppedService();
            }

            if (!Model.IsBuilding)
            {
                _buildDropDown.SetEnabled(true);
            }

        }

        void SetDefaultBuildToStart()
        {
            _buildDropDown.SetEnabled(!Model.IsBuilding);
            _buildDefaultLabel.text = Constants.GetBuildButtonString(Model.IncludeDebugTools, Constants.BUILD_START);
            _defaultBuildAction = OnClick_BuildAndStart;
        }

        void SetDefaultBuildToReset()
        {
            _buildDropDown.SetEnabled(!Model.IsBuilding);
            _buildDefaultLabel.text = Constants.GetBuildButtonString(Model.IncludeDebugTools, Constants.BUILD_RESET);
            _defaultBuildAction = OnClick_BuildAndReset;
        }

        void SetAsStoppedService()
        {
            _startButton.SetEnabled(!Model.IsBuilding);
            _startButton.text = Constants.START;
            SetDefaultBuildToStart();
        }

        void SetAsRunningService()
        {
            _startButton.SetEnabled(!Model.IsBuilding);
            _startButton.text = Constants.STOP;
            SetDefaultBuildToReset();
        }

        private void OnPopoutButton_Clicked()
        {
            if (Model.AreLogsAttached)
            {
                Model.DetachLogs();
            }
            else
            {
                Model.AttachLogs();
            }
        }

        private void CreateLogSection(bool areLogsAttached)
        {
            _logElement?.Destroy();
            _logContainerElement.Clear();
            if (areLogsAttached)
            {
                CreateLogElement();
            }
        }

        private void CreateLogElement()
        {
            _logElement = new LogVisualElement {Model = Model};
            _logContainerElement.Add(_logElement);
            _logElement.Refresh();
        }
          
          
        private void UpdateRemoteStatusIcon()
        {
            _remoteStatusIcon.ClearClassList();

            string statusRemote = "remoteDeploying";
            string statusClassName;
            switch (statusRemote)
            {
                case "remoteDeploying":
                    statusClassName = "remoteDeploying";
                    _remoteStatusLabel.text = "Remote Deploying";
                    break;
                case "remoteRunning":
                    statusClassName = "remoteRunning";
                    _remoteStatusLabel.text = "Remote Running";
                    break;
                default:
                    statusClassName = "remoteStopped";
                    _remoteStatusLabel.text = "Remote Stopped";
                    break;
            }
            _remoteStatusIcon.AddToClassList(statusClassName);

        }
        
        private void UpdateStatusIcon()
        {
            _statusIcon.ClearClassList();

            string statusClassName;

            string status = Model.Builder.IsRunning ? "localRunning" :
                Model.Builder.IsBuilding ? "localBuilding" : "localStopped";
            switch (status)
            {
                case "localRunning":
                    _statusLabel.text = "Local Running";
                    statusClassName = "localRunning";
                    break;
                case "localBuilding":
                    statusClassName = "localBuilding";
                    _statusLabel.text = "Local Building";
                    break;
                case "localStopped":
                    statusClassName = "localStopped";
                    _statusLabel.text = "Local Stopped";
                    break;
                default:
                    statusClassName = "different";
                    _statusLabel.text = "Different";
                    break;
            }
            _statusIcon.AddToClassList(statusClassName);
        }

        private void HandleBuildButtonClicked(ContextualMenuPopulateEvent evt)
        {
            if (_mouseOverBuildDropdown)
            {
                evt.menu.BeamableAppendAction("Build", pos => HandleBuildClicked());
                evt.menu.BeamableAppendAction(Model.IncludeDebugTools
                    ? Constants.BUILD_DISABLE_DEBUG
                    : Constants.BUILD_ENABLE_DEBUG, pos => {
                    Model.IncludeDebugTools = !Model.IncludeDebugTools;
                    UpdateStartAndBuildButtons();
                } );
            }
            else
            {
                DisableBuildAndStart();
                _defaultBuildAction?.Invoke();
            }
        }

        private void OnClick_BuildAndStart()
        {
            DisableBuildAndStart();
            Model.BuildAndStart();
        }

        private void OnClick_BuildAndReset()
        {
            DisableBuildAndStart();
            Model.BuildAndRestart();
        }

        void EnableBuildAndStart()
        {
            _buildDropDown.SetEnabled(true);
            _startButton.SetEnabled(true);
        }

        void DisableBuildAndStart()
        {
            _buildDropDown.SetEnabled(false);
            _startButton.SetEnabled(false);
        }

        private void SetupProgressBarForBuildAndStart(Task task) {
            var loadingBarUpdater = new MergedBarUpdater(_loadingBar, "Build and Run");
            new StepLogParser(loadingBarUpdater.CreateDummyLoadingBar(), Model, task);
            new RunImageLogParser(loadingBarUpdater.CreateDummyLoadingBar(), Model);
        }

        private void SetupProgressBarForBuildAndRestart(Task task) {
            var loadingBarUpdater = new MergedBarUpdater(_loadingBar, "Build and Rerun");
            new StepLogParser(loadingBarUpdater.CreateDummyLoadingBar(), Model, task);
            new RunImageLogParser(loadingBarUpdater.CreateDummyLoadingBar(), Model);
            new StopImageLogParser(loadingBarUpdater.CreateDummyLoadingBar(), Model);
        }

        private void SetupProgressBarForBuild(Task task) {
            new StepLogParser(_loadingBar, Model, task);
        }

        private void SetupProgressBarForStart(Task task) {
            new RunImageLogParser(_loadingBar, Model);
        }

        private void SetupProgressBarForStop(Task task) {
            new StopImageLogParser(_loadingBar, Model);
        }

        private void SetupProgressBarForDeployment(ManifestModel _) {
            new DeployMSLogParser(_loadingBar, Model);
        }
    }

    public class PopupBtn
    {
    }
}