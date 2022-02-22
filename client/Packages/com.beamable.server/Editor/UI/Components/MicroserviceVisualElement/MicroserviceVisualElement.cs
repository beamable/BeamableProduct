using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using System;
using System.Threading.Tasks;
using static Beamable.Common.Constants.Features.Services;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class MicroserviceVisualElement : ServiceBaseVisualElement
	{
		public new class UxmlFactory : UxmlFactory<MicroserviceVisualElement, UxmlTraits>
		{ }
		protected override string ScriptName => nameof(MicroserviceVisualElement);

		private Action _defaultBuildAction;
		private bool _mouseOverBuildDropdown;

		private Label _buildDefaultLabel;
		private Button _buildDropDown;
		private Image _buildDropDownIcon;
		private MicroserviceModel _microserviceModel;

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (_microserviceModel == null) return;

			_microserviceModel.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
			_microserviceModel.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
			_microserviceModel.OnBuild -= SetupProgressBarForBuild;
			_microserviceModel.OnDockerLoginRequired -= LoginToDocker;
			_microserviceModel.ServiceBuilder.OnIsBuildingChanged -= OnIsBuildingChanged;
			_microserviceModel.ServiceBuilder.OnLastImageIdChanged -= HandleLastImageIdChanged;
		}
		protected override void QueryVisualElements()
		{
			base.QueryVisualElements();
			_buildDropDown = Root.Q<Button>("buildDropDown");
			_buildDefaultLabel = _buildDropDown.Q<Label>();
			_buildDropDownIcon = _buildDropDown.Q<Image>();
			_microserviceModel = (MicroserviceModel)Model;
		}
		protected override void UpdateVisualElements()
		{
			base.UpdateVisualElements();
			_buildDropDownIcon.RegisterCallback<MouseEnterEvent>(evt => _mouseOverBuildDropdown = true);
			_buildDropDownIcon.RegisterCallback<MouseLeaveEvent>(evt => _mouseOverBuildDropdown = false);
			var buildBtnManipulator = new ContextualMenuManipulator(HandleBuildButtonClicked);
			buildBtnManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_buildDropDown.clickable.activators.Clear();
			_buildDropDown.AddManipulator(buildBtnManipulator);

			_microserviceModel.OnBuildAndStart -= SetupProgressBarForBuildAndStart;
			_microserviceModel.OnBuildAndStart += SetupProgressBarForBuildAndStart;
			_microserviceModel.OnBuildAndRestart -= SetupProgressBarForBuildAndRestart;
			_microserviceModel.OnBuildAndRestart += SetupProgressBarForBuildAndRestart;
			_microserviceModel.OnBuild -= SetupProgressBarForBuild;
			_microserviceModel.OnBuild += SetupProgressBarForBuild;
			_microserviceModel.OnDockerLoginRequired -= LoginToDocker;
			_microserviceModel.OnDockerLoginRequired += LoginToDocker;

			_microserviceModel.ServiceBuilder.OnIsBuildingChanged -= OnIsBuildingChanged;
			_microserviceModel.ServiceBuilder.OnIsBuildingChanged += OnIsBuildingChanged;
			_microserviceModel.ServiceBuilder.OnLastImageIdChanged -= HandleLastImageIdChanged;
			_microserviceModel.ServiceBuilder.OnLastImageIdChanged += HandleLastImageIdChanged;
			_microserviceModel.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
			_microserviceModel.OnRemoteReferenceEnriched += OnServiceReferenceChanged;
		}
		private void LoginToDocker(Promise<Unit> onLogin)
		{
			DockerLoginVisualElement.ShowUtility().Then(onLogin.CompleteSuccess).Error(onLogin.CompleteError);
		}
		private void OnIsBuildingChanged(bool isBuilding)
		{
			UpdateLocalStatus();
		}
		private void HandleLastImageIdChanged(string newId)
		{
			UpdateLocalStatus();
		}
		private void OnServiceReferenceChanged(ServiceReference serviceReference)
		{
			UpdateRemoteStatusIcon();
		}
		protected override void UpdateRemoteStatusIcon()
		{
			_remoteStatusIcon.ClearClassList();
			bool remoteEnabled = _microserviceModel.RemoteReference?.enabled ?? false;
			string statusClassName = remoteEnabled ? "remoteEnabled" : "remoteDisabled";
			_remoteStatusIcon.tooltip = remoteEnabled ? REMOTE_ENABLED : REMOTE_NOT_ENABLED;
			_remoteStatusIcon.AddToClassList(statusClassName);
		}
		protected override void UpdateLocalStatus()
		{
			base.UpdateLocalStatus();
			UpdateLocalStatusIcon(_microserviceModel.IsRunning, _microserviceModel.IsBuilding);
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
				evt.menu.BeamableAppendAction("Build", pos => _microserviceModel.Build());
				evt.menu.BeamableAppendAction(_microserviceModel.IncludeDebugTools
					? BUILD_DISABLE_DEBUG
					: BUILD_ENABLE_DEBUG, pos =>
				{
					_microserviceModel.IncludeDebugTools = !_microserviceModel.IncludeDebugTools;
					UpdateLocalStatus();
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
			_stopButton.visible = Model.IsRunning;
			_buildDefaultLabel.text = GetBuildButtonString(_microserviceModel.IncludeDebugTools,
				_microserviceModel.IsRunning ? BUILD_RESET : BUILD_START);

			if (_microserviceModel.IsRunning)
			{
				_defaultBuildAction = () => _microserviceModel.BuildAndRestart();
			}
			else
			{
				_defaultBuildAction = () => _microserviceModel.BuildAndStart();
			}
			_stopButton.SetEnabled(_microserviceModel.ServiceBuilder.HasImage && !_microserviceModel.IsBuilding);
			_buildDropDown.SetEnabled(!_microserviceModel.IsBuilding);
		}
	}
}
