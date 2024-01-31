using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server;
using Beamable.Server.Editor.Usam;
using System.Linq;
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
		public IBeamoServiceDefinition Model { get; set; }

		protected LoadingBarElement _loadingBar;
		protected VisualElement _moreBtn;
		protected MicroserviceVisualElementSeparator _separator;
		private VisualElement _logContainerElement;
		private LogVisualElement _logElement;
		private VisualElement _header;
		private VisualElement _rootVisualElement;
		private VisualElement _mainParent;
		private VisualElement _serviceCard;
		private Button _foldButton;
		private VisualElement _foldIcon;
		private Label _serviceName;
		private VisualElement _openDocsBtn;
		private VisualElement _openScriptBtn;

		public new class UxmlFactory : UxmlFactory<StandaloneMicroserviceVisualElement, UxmlTraits> { }

		public StandaloneMicroserviceVisualElement() :
			base(
				$"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/UI2/Components/{nameof(StandaloneMicroserviceVisualElement)}/{nameof(StandaloneMicroserviceVisualElement)}")
		{ }

		public override void Refresh()
		{
			_visualsModel = Context.ServiceScope.GetService<UsamDataModel>().GetModel(Model.BeamoId);
			base.Refresh();
			QueryVisualElements();
			UpdateVisualElements();
			var query = Root.Query().Where(v => v is IBeamoServiceElement).ToList().Select(v => v as IBeamoServiceElement);
			foreach (var el in query)
			{
				el?.FeedData(Model, Context);
			}
		}

		protected virtual void QueryVisualElements()
		{
			_rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
			Root.Q("microserviceNewTitle")?.RemoveFromHierarchy();
			_moreBtn = Root.Q<VisualElement>("moreBtn");
			_logContainerElement = Root.Q<VisualElement>("logContainer");
			_header = Root.Q("logHeader");
			_separator = Root.Q<MicroserviceVisualElementSeparator>("separator");
			_serviceCard = Root.Q("serviceCard");
			_loadingBar = new LoadingBarElement();
			_serviceCard.Add(_loadingBar);
			_foldButton = Root.Q<Button>("foldButton");
			_foldIcon = Root.Q("foldIcon");
			_serviceName = Root.Q<Label>("serviceName");
			_openDocsBtn = Root.Q<VisualElement>("openDocsBtn");
			_openScriptBtn = Root.Q<VisualElement>("openScriptBtn");
			_mainParent = _rootVisualElement.parent.parent;
		}
		protected virtual void UpdateVisualElements()
		{
			_loadingBar.Refresh();
			new BeamoStepLogParser(_loadingBar, Model.Builder, Model.BeamoId);
			_loadingBar.Hidden = true;
			_loadingBar.PlaceBehind(Root.Q("SubTitle"));

			var manipulator = new ContextualMenuManipulator(_visualsModel.PopulateMoreDropdown);
			manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_moreBtn.tooltip = Constants.Tooltips.Microservice.MORE;
			_moreBtn.AddManipulator(manipulator);

			_openScriptBtn.AddManipulator(new Clickable(() =>
			{
				BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OpenMicroserviceFile(Model.BeamoId);
			}));
			_openScriptBtn.tooltip = "Open C# Code";

			_openDocsBtn.AddManipulator(new Clickable(OpenLocalDocs));
			_openDocsBtn.SetEnabled(Model.IsRunningLocally);
			_openDocsBtn.tooltip = "View Documentation";

			_serviceName.text = _serviceName.tooltip = Model.BeamoId;

			_visualsModel.OnLogsAttachmentChanged -= CreateLogSection;
			_visualsModel.OnLogsAttachmentChanged += CreateLogSection;
			//
			Model.Builder.OnIsRunningChanged -= HandleIsRunningChanged;
			Model.Builder.OnIsRunningChanged += HandleIsRunningChanged;
			Model.Builder.OnStartingFinished -= HandleProgressFinished;
			Model.Builder.OnStartingFinished += HandleProgressFinished;
			_separator.Setup(OnDrag);
			_separator.Refresh();

			_foldButton.clickable.clicked += HandleCollapseButton;
			_mainParent.AddToClassList("folded");
			_rootVisualElement.AddToClassList("folded");

			CreateLogSection(_visualsModel.AreLogsAttached);
			UpdateLocalStatus();
			ChangeCollapseState();

			if (Model.ExistLocally)
			{
				return;
			}

			Root.Q<Button>("startBtn").RemoveFromHierarchy();
			Root.Q<VisualElement>("logContainer").RemoveFromHierarchy();
			Root.Q("collapseContainer")?.RemoveFromHierarchy();
			Root.Q("statusSeparator")?.RemoveFromHierarchy();
			Root.Q<VisualElement>("openDocsBtn")?.RemoveFromHierarchy();
			Root.Q<VisualElement>("openScriptBtn")?.RemoveFromHierarchy();
			Root.Q<MicroserviceVisualElementSeparator>("separator")?.RemoveFromHierarchy();
			Root.Q("foldContainer").visible = false;
			Root.Q<VisualElement>("mainVisualElement").style.SetHeight(DEFAULT_HEADER_HEIGHT);
		}

		private void HandleIsRunningChanged(bool _)
		{
			UpdateLocalStatus();
		}

		protected void HandleProgressFinished(bool gotError)
		{
			_header.EnableInClassList("failed", gotError);

		}

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

			_rootVisualElement.style.height = new StyleLength(DETACHED_HEIGHT);
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

		private void SetHeight(float newHeight)
		{
			_visualsModel.ElementHeight = Mathf.Clamp(newHeight, MIN_HEIGHT, MAX_HEIGHT);
#if UNITY_2019_1_OR_NEWER
			_rootVisualElement.style.height = new StyleLength(_visualsModel.ElementHeight);
#elif UNITY_2018
            _rootVisualElement.style.height = StyleValue<float>.Create(_visualsModel.ElementHeight);
#endif
		}
		protected virtual void UpdateLocalStatus()
		{
			_header.EnableInClassList("running", Model.Builder.IsRunning);
			_openDocsBtn.SetEnabled(Model.Builder.IsRunning);
		}

		public void OpenLocalDocs()
		{
			var de = BeamEditorContext.Default;
			var url = $"{BeamableEnvironment.PortalUrl}/{de.CurrentCustomer.Alias}/games/{de.ProductionRealm.Pid}/realms/{de.CurrentRealm.Pid}/microservices/{Model.BeamoId}/docs?prefix={MicroserviceIndividualization.Prefix}&refresh_token={de.Requester.Token.RefreshToken}";
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
