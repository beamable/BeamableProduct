using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using System;
using System.Globalization;
using System.Threading.Tasks;
using static Beamable.Common.Constants.Features.Services;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class MicroserviceVisualElement : ServiceBaseVisualElement
	{
		public new class UxmlFactory : UxmlFactory<MicroserviceVisualElement, UxmlTraits>
		{ }
		protected override string ScriptName => nameof(MicroserviceVisualElement);

		private MicroserviceModel _microserviceModel;

		public override void Refresh()
		{
			base.Refresh();
		}

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
			_microserviceModel.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
		}
		protected override void QueryVisualElements()
		{
			base.QueryVisualElements();
			_microserviceModel = (MicroserviceModel)Model;
		}
		protected override void UpdateVisualElements()
		{
			base.UpdateVisualElements();
			Root.Q("leftArea")?.RemoveFromHierarchy();
			_startButton.clickable.clicked -= HandleStartButtonClicked;
			_startButton.clickable.clicked += HandleStartButtonClicked;
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
			_remoteStatusIcon.tooltip = remoteEnabled ? Tooltips.Microservice.ICON_REMOTE_RUNNING : Tooltips.Microservice.ICON_REMOTE_DISABLE;
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
		private void HandleStartButtonClicked()
		{
			if (_microserviceModel.IsRunning)
			{
				_microserviceModel.Stop();
			}
			else
			{
				_microserviceModel.BuildAndStart();
			}
		}

		protected override void UpdateButtons()
		{
			base.UpdateButtons();

			var api = BeamEditorContext.Default;
			if (!api.IsAuthenticated)
				return;

			ChangeStartButtonState(!_microserviceModel.IsBuilding);
		}

		public override void ChangeStartButtonState(bool isOn, string enabledTooltip = null, string disabledTooltip = null)
		{
			enabledTooltip = enabledTooltip ?? GetBuildButtonString(_microserviceModel.IncludeDebugTools,
			                                                        _microserviceModel.IsRunning ? STOP : Tooltips.Microservice.PLAY_MICROSERVICE);
			base.ChangeStartButtonState(isOn, enabledTooltip, disabledTooltip);
		}
	}
}
