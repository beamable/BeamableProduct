using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.UI.Buss;
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
    public abstract class ServiceBaseVisualElement : MicroserviceComponent
    {
        protected ServiceBaseVisualElement() : base(nameof(ServiceBaseVisualElement))
        {
        }
        
        public ServiceModelBase Model { get; set; }
        protected abstract string ScriptName { get; }

        private const float MIN_HEIGHT = 200.0f;
        private const float MAX_HEIGHT = 500.0f;
        private const float DETACHED_HEIGHT = 100.0f;
        private const float DEFAULT_HEIGHT = 300.0f;
        
        private float _storedHeight = 0;
        
        protected Button _stopButton;
        protected LoadingBarElement _loadingBar;
        protected VisualElement _statusIcon;
        protected Label _statusLabel;
        protected Label _remoteStatusLabel;
        protected VisualElement _remoteStatusIcon;
        
        private Label _nameTextField;
        private Button _moreBtn;
        private BeamableCheckboxVisualElement _checkbox;
        private VisualElement _logContainerElement;
        private LogVisualElement _logElement;
        private MicroserviceVisualElementSeparator _separator;
        private VisualElement _header;
        private VisualElement _rootVisualElement;

        public Action OnServiceStartFailed { get; set; }
        public Action OnServiceStopFailed { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Microservices.onBeforeDeploy -= SetupProgressBarForDeployment;

            if (Model == null) return;
            
            Model.OnStart -= SetupProgressBarForStart;
            Model.OnStop -= SetupProgressBarForStop;
            Model.OnLogsAttachmentChanged -= CreateLogSection;
            Model.Builder.OnIsRunningChanged -= HandleIsRunningChanged;
        }
        public override void Refresh()
        {
            base.Refresh();
            name = Model.Name;
            QueryVisualElements();
            InjectStyleSheets();
            UpdateVisualElements();
        }
        protected virtual void QueryVisualElements()
        {
            _rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
            Root.Q<Button>("cancelBtn").RemoveFromHierarchy();
            Root.Q("microserviceNewTitle")?.RemoveFromHierarchy();
            _nameTextField = Root.Q<Label>("microserviceTitle");
            _stopButton = Root.Q<Button>("stopBtn");
            _moreBtn = Root.Q<Button>("moreBtn");
            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _logContainerElement = Root.Q<VisualElement>("logContainer");
            _statusLabel = Root.Q<Label>("statusTitle");
            _remoteStatusLabel = Root.Q<Label>("remoteStatusTitle");
            _statusIcon = Root.Q<VisualElement>("statusIcon");
            _remoteStatusIcon = Root.Q<VisualElement>("remoteStatusIcon");
            _header = Root.Q("logHeader");
            _separator = Root.Q<MicroserviceVisualElementSeparator>("separator");
            _loadingBar = new LoadingBarElement();
            _rootVisualElement.Add(_loadingBar);
        }
        private void InjectStyleSheets()
        {
            if (string.IsNullOrWhiteSpace(ScriptName)) return;
            _rootVisualElement.AddStyleSheet($"{Constants.COMP_PATH}/{ScriptName}/{ScriptName}.uss");
        }
        protected virtual void UpdateVisualElements()
        {
            _loadingBar.Hidden = true;
            _loadingBar.Refresh();
            _loadingBar.PlaceBehind(Root.Q("SubTitle"));

            Model.OnStart -= SetupProgressBarForStart;
            Model.OnStart += SetupProgressBarForStart;
            Model.OnStop -= SetupProgressBarForStop;
            Model.OnStop += SetupProgressBarForStop;

            Microservices.onBeforeDeploy -= SetupProgressBarForDeployment;
            Microservices.onBeforeDeploy += SetupProgressBarForDeployment;

            _nameTextField.text = Model.Name;
            _stopButton.clickable.clicked += HandleStopButtonClicked;
            
            var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
            manipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            _moreBtn.clickable.activators.Clear();
            _moreBtn.AddManipulator(manipulator);
            _moreBtn.tooltip = "More...";

            _checkbox.Refresh();
            _checkbox.SetWithoutNotify(Model.IsSelected);
            Model.OnSelectionChanged += _checkbox.SetWithoutNotify;
            _checkbox.OnValueChanged += b => Model.IsSelected = b;
            
            Model.OnLogsAttachmentChanged -= CreateLogSection;
            Model.OnLogsAttachmentChanged += CreateLogSection;

            Model.Builder.OnIsRunningChanged -= HandleIsRunningChanged;
            Model.Builder.OnIsRunningChanged += HandleIsRunningChanged;

            Root.Q("dependentServicesContainer").visible = MicroserviceConfiguration.Instance.EnableStoragePreview;
            
            _separator.Setup(OnDrag);
            _separator.Refresh();
            
            UpdateButtons();
            CreateLogSection(Model.AreLogsAttached);
            UpdateStatusIcon();
            UpdateRemoteStatusIcon();
            UpdateHeaderColor();
            UpdateModel();
        }
        protected abstract void UpdateStatusIcon();
        protected abstract void UpdateRemoteStatusIcon();
        protected virtual void UpdateButtons()
        {
            _stopButton.visible = Model.IsRunning;
        }
        private async void UpdateModel()
        {
            await Model.Builder.CheckIfIsRunning();
        }
        private void OnDrag(float value)
        {
            if (!Model.AreLogsAttached)
            {
                return;
            }

            var layoutHeight = _rootVisualElement.layout.height;
            var newHeight = layoutHeight + value;

            newHeight = Mathf.Clamp(newHeight, MIN_HEIGHT, MAX_HEIGHT);
#if UNITY_2019_1_OR_NEWER
            _rootVisualElement.style.height = new StyleLength(newHeight);
#elif UNITY_2018
            _rootVisualElement.style.height = StyleValue<float>.Create(newHeight);
#endif
        }
        private void HandleStopButtonClicked()
        {
            if (Model.IsRunning)
            {
                Model.Stop();
            }
        }
        private void HandleIsRunningChanged(bool isRunning)
        {
            UpdateButtons();
            UpdateStatusIcon();
            UpdateHeaderColor();
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
            _rootVisualElement.style.height = new StyleLength(_storedHeight > 0 ? _storedHeight : DEFAULT_HEIGHT);
#elif UNITY_2018
                _rootVisualElement.style.height =
                    StyleValue<float>.Create(_storedHeight > 0 ? _storedHeight : DEFAULT_HEIGHT);
#endif
                _storedHeight = 0;
            }
        }
        private void CreateLogElement()
        {
            _logElement = new LogVisualElement { Model = Model };
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
            _rootVisualElement.style.height = new StyleLength(DETACHED_HEIGHT);
#elif UNITY_2018
            _rootVisualElement.style.height =
                StyleValue<float>.Create(DETACHED_HEIGHT);
#endif
        }
        private void SetupProgressBarForStart(Task task)
        {
            // We have two ways. Either store reference or return instance as event parameter
            new RunImageLogParser(_loadingBar, Model) { OnFailure = OnStartFailed };
        }
        private void OnStartFailed()
        {
            OnServiceStartFailed?.Invoke();
        }
        private void SetupProgressBarForStop(Task task)
        {
            new StopImageLogParser(_loadingBar, Model) { OnFailure = OnStopFailed };
        }
        private void OnStopFailed()
        {
            OnServiceStopFailed?.Invoke();
        }
        private void SetupProgressBarForDeployment(ManifestModel _, int __)
        {
            new GroupLoadingBarUpdater("Build and Deploy", _loadingBar, false,
                new StepLogParser(new VirtualLoadingBar(), Model, null),
                new DeployMSLogParser(new VirtualLoadingBar(), Model)
            );
        }
    }
}