using Beamable.Common;
using Beamable.Editor.Microservice.UI;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System.Runtime.Remoting.Contexts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2
{
	public class UsamWindow : BeamEditorWindow<UsamWindow>
	{
		private CodeService _codeService;
		private UsamDataModel _dataModel;

		static UsamWindow()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Usam Editor",
				FocusOnShow = false,
				DockPreferenceTypeName = inspector.AssemblyQualifiedName,
				RequireLoggedUser = true,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Usam Editor %g",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		public override bool ShowLoading => true;

		private VisualElement _windowRoot;
		private ActionBarVisualElement _actionBarVisualElement;
		private MicroserviceBreadcrumbsVisualElement _microserviceBreadcrumbsVisualElement;
		private CreateServiceVisualElement _createServiceElement;

		private ScrollView _scrollView;

		protected override void Build()
		{
			// ActiveContext.ServiceScope.
			var root = this.GetRootVisualContainer();
			root.Clear();

			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI}/MicroserviceWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI}/MicroserviceWindow.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();
			_windowRoot.userData = ActiveContext.ServiceScope;

			root.Add(_windowRoot);

			_actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
			_actionBarVisualElement.Refresh();
			_actionBarVisualElement.UpdateButtonsState(_codeService.ServiceDefinitions.Count);

			_actionBarVisualElement.OnRefreshButtonClicked += HandleRefreshButtonClicked;
			_microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
			_microserviceBreadcrumbsVisualElement.Refresh();
			_scrollView = new ScrollView(ScrollViewMode.Vertical);
			var emptyContainer = new VisualElement { name = "listRoot" };

			var microserviceContentVisualElement = root.Q("microserviceContentVisualElement");
			microserviceContentVisualElement.Add(new VisualElement { name = "announcementList" });

			_createServiceElement = new CreateServiceVisualElement();
			_createServiceElement.SetHidden(true);
			microserviceContentVisualElement.Add(_createServiceElement);

			microserviceContentVisualElement.Add(_scrollView);
			_scrollView.Add(emptyContainer);
			OnLoad().Then(_ =>
			{
				_dataModel = Scope.GetService<UsamDataModel>();

				foreach (BeamoServiceDefinition beamoServiceDefinition in _codeService.ServiceDefinitions)
				{
					var model = _dataModel.GetModel(beamoServiceDefinition.BeamoId);
					var el = new StandaloneMicroserviceVisualElement() { Model = beamoServiceDefinition };
					emptyContainer.Add(el);

					model.OnLogsDetached += () => { ServiceLogWindow.ShowService(model); };
					if (!model.AreLogsAttached)
					{
						ServiceLogWindow.ShowService(model);
					}
					el.Refresh();
				}
			});

			_actionBarVisualElement.OnCreateNewClicked += HandleCreateNewButtonClicked;
			_actionBarVisualElement.OnPublishClicked += HandlePublishButtonClicked;
		}

		private void HandlePublishButtonClicked()
		{
			PublishStandaloneWindow.ShowPublishWindow(this, ActiveContext);
		}

		private void ShowDockerNotRunningAnnouncement()
		{
			var dockerAnnouncement = new DockerAnnouncementModel() { IsDockerInstalled = true, OnInstall = Build };
			var announcementList = _windowRoot.Q<VisualElement>("announcementList");
			announcementList.Clear();

			var element = new DockerAnnouncementVisualElement() { DockerAnnouncementModel = dockerAnnouncement };
			announcementList.Add(element);
			element.Refresh();
		}

		private void HandleRefreshButtonClicked()
		{
			_codeService = Scope.GetService<CodeService>();
			_codeService.RefreshServices().Then(_ => Build()).Error(Debug.LogError);
		}

		private void HandleCreateNewButtonClicked(ServiceType serviceType)
		{
			_createServiceElement.ServiceType = serviceType;
			_createServiceElement.Refresh(_actionBarVisualElement.Refresh);
			EditorApplication.delayCall += () => _scrollView.verticalScroller.value = 0f;
		}

		public override async Promise OnLoad()
		{
			_codeService = Scope.GetService<CodeService>();


			await _codeService.OnReady;
		}
	}
}
