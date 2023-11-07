using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.UI.Components;
using Beamable.Modules.Generics;
using Beamable.Server;
using Beamable.Server.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Components
{
	public class StandaloneMicroserviceVisualElement : BeamableVisualElement
	{
		
		private const float MIN_HEIGHT = 240.0f;
		private const float MAX_HEIGHT = 500.0f;
		private const float DETACHED_HEIGHT = 30.0f;
		protected const float DEFAULT_HEADER_HEIGHT = 30.0f;
		private MicroserviceVisualsModel _visualsModel;
		public ServiceInfo Info { get; set; }
		
		protected LoadingBarElement _loadingBar;
		protected VisualElement _moreBtn;
		protected Button _startButton;
		protected MicroserviceVisualElementSeparator _separator;
		private VisualElement _logContainerElement;
		private LogVisualElement _logElement;
		private VisualElement _header;
		private VisualElement _rootVisualElement;
		private VisualElement _mainParent;
		private VisualElement _serviceCard;
		private Button _foldButton;
		private VisualElement _foldIcon;
		protected Image _serviceIcon;
		private Label _serviceName;
		private VisualElement _openDocsBtn;
		private VisualElement _openScriptBtn;
		
		public new class UxmlFactory : UxmlFactory<StandaloneMicroserviceVisualElement, UxmlTraits> { }

		public StandaloneMicroserviceVisualElement() :
			base(
				$"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/UI2/Components/{nameof(StandaloneMicroserviceVisualElement)}/{nameof(StandaloneMicroserviceVisualElement)}") { }

		public override void Refresh()
		{
			_visualsModel = MicroserviceVisualsModel.GetModel(Info.name);
			base.Refresh();
			QueryVisualElements();
			UpdateVisualElements();
		}

		protected virtual void QueryVisualElements()
		{
			_rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
			Root.Q("microserviceNewTitle")?.RemoveFromHierarchy();
			_moreBtn = Root.Q<VisualElement>("moreBtn");
			_startButton = Root.Q<Button>("startBtn");
			_logContainerElement = Root.Q<VisualElement>("logContainer");
			_header = Root.Q("logHeader");
			_separator = Root.Q<MicroserviceVisualElementSeparator>("separator");
			_serviceCard = Root.Q("serviceCard");
			_loadingBar = new LoadingBarElement();
			_serviceCard.Add(_loadingBar);
			_foldButton = Root.Q<Button>("foldButton");
			_foldIcon = Root.Q("foldIcon");
			_serviceIcon = Root.Q<Image>("serviceIcon");
			_serviceName = Root.Q<Label>("serviceName");
			_openDocsBtn = Root.Q<VisualElement>("openDocsBtn");
			_openScriptBtn = Root.Q<VisualElement>("openScriptBtn");
			_mainParent = _rootVisualElement.parent.parent;
		}
		protected virtual void UpdateVisualElements()
		{
			_loadingBar.Hidden = true;
			_loadingBar.Refresh();
			_loadingBar.PlaceBehind(Root.Q("SubTitle"));

			// var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
			// manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_moreBtn.tooltip = Constants.Tooltips.Microservice.MORE;
			// _moreBtn.AddManipulator(manipulator);

			_openScriptBtn.AddManipulator(new Clickable(Info.OpenCode));
			_openScriptBtn.tooltip = "Open C# Code";

			_openDocsBtn.AddManipulator(new Clickable(OpenLocalDocs));
			// _openDocsBtn.SetEnabled(Model.IsRunning);
			_openDocsBtn.tooltip = "View Documentation";
			
			_serviceName.text = _serviceName.tooltip = Info.name;

			if (_serviceIcon != null)
			{
				_serviceIcon.tooltip = Constants.Tooltips.Microservice.MICROSERVICE;
				// _serviceIcon.tooltip = Model.Descriptor.ServiceType == ServiceType.MicroService ?
				// 	Constants.Tooltips.Microservice.MICROSERVICE : Constants.Tooltips.Microservice.STORAGE_OBJECT;
			}

			// Model.OnLogsAttachmentChanged -= CreateLogSection;
			// Model.OnLogsAttachmentChanged += CreateLogSection;
			//
			// Model.Builder.OnIsRunningChanged -= HandleIsRunningChanged;
			// Model.Builder.OnIsRunningChanged += HandleIsRunningChanged;
			//
			// Model.Builder.OnBuildingProgress -= HandleStartingProgress;
			// Model.Builder.OnBuildingProgress += HandleStartingProgress;

			// _separator.Setup(OnDrag);
			_separator.Refresh();

			_foldButton.clickable.clicked += HandleCollapseButton;
			_mainParent.AddToClassList("folded");
			_rootVisualElement.AddToClassList("folded");

			CreateLogSection(true);
			UpdateLocalStatus();
			UpdateRemoteStatusIcon();
			ChangeCollapseState();
			UpdateModel();
		}
		protected void UpdateModel()
		{
			// if (!_isDockerRunning)
			// 	return;
			//
			// await Model.Builder.CheckIfIsRunning();
			UpdateLocalStatus();
		}
		protected void HandleProgressFinished(bool gotError) => _header.EnableInClassList("failed", gotError);

		private void CreateLogSection(bool areLogsAttached)
		{
			_logElement?.Destroy();
			_logContainerElement.Clear();
			if (areLogsAttached)
			{
				CreateLogElement();
				SetHeight(_visualsModel.ElementHeight);
			}
		}
		private void CreateLogElement()
		{
			_logElement = new LogVisualElement
			{
				Model = _visualsModel
			};
			_logElement.AddToClassList("logElement");
			_logElement.OnDetachLogs += OnLogsDetached;
			_logContainerElement.Add(_logElement);
			_logElement.Refresh();
		}
		private void OnLogsDetached()
		{
			_logElement.OnDetachLogs -= OnLogsDetached;
			_visualsModel.ElementHeight = _rootVisualElement.layout.height;

#if UNITY_2019_1_OR_NEWER
			_rootVisualElement.style.height = new StyleLength(DETACHED_HEIGHT);
#elif UNITY_2018
            _rootVisualElement.style.height =
                StyleValue<float>.Create(DETACHED_HEIGHT);
#endif
		}
		private void OnDrag(float value)
		{
			if (!_visualsModel.AreLogsAttached)
			{
				return;
			}

			var layoutHeight = _rootVisualElement.layout.height;
			SetHeight(layoutHeight + value);
		}
		
		protected void UpdateRemoteStatusIcon(string customStatusClassName = "")
		{
			_serviceIcon.ClearClassList();
			
			var statusClassName = string.IsNullOrWhiteSpace(customStatusClassName)
				? $"{ServiceType.MicroService}_remoteDisabled"
				// ? IsRemoteEnabled
				// 	? $"{ServiceType.MicroService}_remoteEnabled"
				// 	: $"{ServiceType.MicroService}_remoteDisabled"
				: $"{ServiceType.MicroService}_{customStatusClassName}";

			// _serviceIcon.tooltip = IsRemoteEnabled ? Tooltips.Microservice.ICON_REMOTE_RUNNING : Tooltips.Microservice.ICON_REMOTE_DISABLE;
			_serviceIcon.AddToClassList(statusClassName);
		}
		private void SetHeight(float newHeight)
		{
			_visualsModel.ElementHeight = Mathf.Clamp(newHeight, MIN_HEIGHT, MAX_HEIGHT);
#if UNITY_2019_1_OR_NEWER
			_rootVisualElement.style.height = new StyleLength(_visualsModel.ElementHeight);
#elif UNITY_2018
            _rootVisualElement.style.height = StyleValue<float>.Create(_visualsModel.ElementHeight);
#endif
		}
		protected virtual void UpdateButtons()
		{
		}
		protected virtual void UpdateLocalStatus()
		{
			// _header.EnableInClassList("running", Model.IsRunning);
			// _openDocsBtn.SetEnabled(Model.IsRunning);
			UpdateButtons();
		}

		public void OpenLocalDocs()
		{
			var de = BeamEditorContext.Default;
			var url = $"{BeamableEnvironment.PortalUrl}/{de.CurrentCustomer.Alias}/games/{de.ProductionRealm.Pid}/realms/{de.CurrentRealm.Pid}/microservices/{Info.name}/docs?prefix={MicroserviceIndividualization.Prefix}&refresh_token={de.Requester.Token.RefreshToken}";
			Application.OpenURL(url);
		}
		

		private void HandleCollapseButton()
		{
			_visualsModel.IsCollapsed = !_visualsModel.IsCollapsed;
			ChangeCollapseState();
		}
		private void ChangeCollapseState()
		{
			_logContainerElement.EnableInClassList("--positionHidden", _visualsModel.IsCollapsed);
			_separator.EnableInClassList("--positionHidden", _visualsModel.IsCollapsed);
			_foldIcon.EnableInClassList("foldIcon", _visualsModel.IsCollapsed);
			_foldIcon.EnableInClassList("unfoldIcon", !_visualsModel.IsCollapsed);
			_rootVisualElement.EnableInClassList("folded", _visualsModel.IsCollapsed);
			_mainParent.EnableInClassList("folded", _visualsModel.IsCollapsed);
		}
	}
}
