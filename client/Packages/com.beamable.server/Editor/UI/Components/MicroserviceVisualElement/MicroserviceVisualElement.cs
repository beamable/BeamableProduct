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
    public class MicroserviceVisualElement : ServiceBaseVisualElement<MicroserviceModel>
    {
        public MicroserviceVisualElement() : base(nameof(MicroserviceVisualElement))
        {
        }
        
        private Action _defaultBuildAction;
        private bool _mouseOverBuildDropdown;
        
        private Label _buildDefaultLabel;
        private Button _buildDropDown;
        private Image _buildDropDownIcon;
        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Model == null) return;
            
            Model.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
            Model.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
            Model.OnBuild -= SetupProgressBarForBuild;
            Model.OnDockerLoginRequired -= LoginToDocker;
            Model.Builder.OnIsBuildingChanged -= OnIsBuildingChanged;
            Model.Builder.OnLastImageIdChanged -= HandleLastImageIdChanged;
        }
        protected override void QueryVisualElements()
        {
            base.QueryVisualElements();
            _buildDropDown = Root.Q<Button>("buildDropDown");
            _buildDefaultLabel = _buildDropDown.Q<Label>();
            _buildDropDownIcon = _buildDropDown.Q<Image>();
        }
        protected override void UpdateVisualElements()
        {
            base.UpdateVisualElements();
            _buildDropDownIcon.RegisterCallback<MouseEnterEvent>(evt => _mouseOverBuildDropdown = true);
            _buildDropDownIcon.RegisterCallback<MouseLeaveEvent>(evt => _mouseOverBuildDropdown = false);
            var buildBtnManipulator = new ContextualMenuManipulator(HandleBuildButtonClicked);
            buildBtnManipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            _buildDropDown.clickable.activators.Clear();
            _buildDropDown.AddManipulator(buildBtnManipulator);
            
            Model.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
            Model.OnBuildAndStart += SetupProgressBarForBuildAndStart;
            Model.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
            Model.OnBuildAndRestart += SetupProgressBarForBuildAndRestart;
            Model.OnBuild -= SetupProgressBarForBuild;
            Model.OnBuild += SetupProgressBarForBuild;
            Model.OnDockerLoginRequired -= LoginToDocker;
            Model.OnDockerLoginRequired += LoginToDocker;
            
            Model.Builder.OnIsBuildingChanged -= OnIsBuildingChanged;
            Model.Builder.OnIsBuildingChanged += OnIsBuildingChanged;
            Model.Builder.OnLastImageIdChanged -= HandleLastImageIdChanged;
            Model.Builder.OnLastImageIdChanged += HandleLastImageIdChanged;
            Model.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
            Model.OnRemoteReferenceEnriched += OnServiceReferenceChanged;
        }
        private void LoginToDocker(Promise<Unit> onLogin)
        {
            DockerLoginVisualElement.ShowUtility().Then(onLogin.CompleteSuccess).Error(onLogin.CompleteError);
        }
        private void OnIsBuildingChanged(bool isBuilding)
        {
            UpdateButtons();
            UpdateStatusIcon();
        }
        private void HandleLastImageIdChanged(string newId)
        {
            UpdateButtons();
        }
        private void OnServiceReferenceChanged(ServiceReference serviceReference)
        {
            UpdateRemoteStatusIcon();
        }
        protected override void UpdateRemoteStatusIcon()
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
        protected override void UpdateStatusIcon()
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
        private void HandleBuildButtonClicked(ContextualMenuPopulateEvent evt)
        {
            if (_mouseOverBuildDropdown)
            {
                evt.menu.BeamableAppendAction("Build", pos => Model.Build());
                evt.menu.BeamableAppendAction(Model.IncludeDebugTools
                    ? Constants.BUILD_DISABLE_DEBUG
                    : Constants.BUILD_ENABLE_DEBUG, pos =>
                {
                    Model.IncludeDebugTools = !Model.IncludeDebugTools;
                    UpdateButtons();
                });
            }
            else
            {
                _defaultBuildAction?.Invoke();
            }
        }
        protected override void UpdateButtons()
        {
            base.UpdateButtons();
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
    }
}