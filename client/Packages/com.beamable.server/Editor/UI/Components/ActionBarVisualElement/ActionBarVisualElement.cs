using Beamable.Editor.Microservice.UI2.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class ActionBarVisualElement : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as ActionBarVisualElement;

			}
		}

		public ActionBarVisualElement() : base(nameof(ActionBarVisualElement))
		{
		}

		public event Action OnPublishClicked;
		public event Action OnRefreshButtonClicked;

		public event Action OnSettingsButtonClicked;
		public event Action<ServiceType> OnCreateNewClicked;

		private Button _refreshButton;
		private Button _createNew;
		private Button _startAll;
		private Button _infoButton;
		private Button _publish;
		private Button _dependencies;
		private CodeService _codeService;

		public event Action OnInfoButtonClicked;

		public bool HasPublishPermissions => BeamEditorContext.Default.Permissions.CanPublishMicroservices;
		bool IsDockerActive => _codeService.IsDockerRunning;

		private List<IBeamoServiceDefinition> _allLocalServices;

		public override void Refresh()
		{
			base.Refresh();

			_codeService = Context.ServiceScope.GetService<CodeService>();
			_allLocalServices = _codeService.ServiceDefinitions.Where(sd => sd.ServiceType == ServiceType.MicroService)
											.ToList();

			_refreshButton = Root.Q<Button>("refreshButton");
			_refreshButton.clickable.clicked += () => { OnRefreshButtonClicked?.Invoke(); };
			_refreshButton.tooltip = Tooltips.Microservice.REFRESH;

			_createNew = Root.Q<Button>("createNew");
			_createNew.clickable.clicked += () => HandleCreateNewButton(_createNew.worldBound);
			_createNew.tooltip = Tooltips.Microservice.ADD_NEW;

			_startAll = Root.Q<Button>("startAll");
			_startAll.clickable.clicked += () => HandlePlayButton(_startAll.worldBound);

			_dependencies = Root.Q<Button>("dependencies");
			_dependencies.clickable.clicked += () => { OnSettingsButtonClicked?.Invoke(); };
			_dependencies.tooltip = Tooltips.Microservice.DEPENDENCIES;

			const string cannotPublishText = "Cannot open Publish Window, fix compilation errors first!";
			_publish = Root.Q<Button>("publish");
			_publish.clickable.clicked += () =>
			{
				if (!NoErrorsValidator.LastCompilationSucceded)
				{
					Debug.LogError(cannotPublishText);
					return;
				}
				OnPublishClicked?.Invoke();
			};
			_publish.tooltip = Tooltips.Microservice.PUBLISH;
			if (!NoErrorsValidator.LastCompilationSucceded)
				_publish.tooltip = cannotPublishText;

			_infoButton = Root.Q<Button>("infoButton");
			_infoButton.clickable.clicked += () => { OnInfoButtonClicked?.Invoke(); };
			_infoButton.tooltip = Tooltips.Microservice.DOCUMENT;

			UpdateButtonsState(_allLocalServices.Count(x => x.ShouldBeEnabledOnRemote));

			Context.OnRealmChange += _ => Refresh();
			Context.OnUserChange += _ => Refresh();
		}

		public void UpdateButtonsState(int servicesAmount)
		{
			var canPublish = IsDockerActive && servicesAmount > 0 && HasPublishPermissions;

			_startAll.SetEnabled(IsDockerActive && servicesAmount > 0);
			_publish.SetEnabled(canPublish);

			if (!canPublish)
			{
				_publish.tooltip = !IsDockerActive ? "Docker is not running." :
					servicesAmount == 0 ? "Nothing to publish." :
					!HasPublishPermissions ? "Require publish permission." : string.Empty;
			}
		}

		private void HandleCreateNewButton(Rect visualElementBounds)
		{
			var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);
			var content = new CreateNewServiceDropdownVisualElement();
			var wnd = BeamablePopupWindow.ShowDropdown("Services", popupWindowRect, new Vector2(140, Enum.GetNames(typeof(ServiceType)).Length * 28), content);
			content.Refresh();
			content.OnCreateNewClicked += serviceType =>
			{
				_createNew.SetEnabled(false);
				OnCreateNewClicked?.Invoke(serviceType);
				wnd.Close();
			};
		}

		private void HandlePlayButton(Rect visualElementBounds)
		{
			var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);
			var services = _allLocalServices.Where(x => x.ShouldBeEnabledOnRemote).ToList();
			var content = new ServicesDropdownVisualElement(services);
			var wnd = BeamablePopupWindow.ShowDropdown("Services", popupWindowRect, new Vector2(200, 50 + services.Count * 24), content);
			content.Refresh();
			content.OnCloseRequest += wnd.Close;
		}
	}
}
