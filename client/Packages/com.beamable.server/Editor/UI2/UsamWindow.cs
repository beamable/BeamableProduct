using Beamable.Common;
using Beamable.Editor.Microservice.UI;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System.Runtime.Remoting.Contexts;
using UnityEditor;
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
			_actionBarVisualElement.OnRefreshButtonClicked += HandleRefreshButtonClicked;

			_microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
			_microserviceBreadcrumbsVisualElement.Refresh();
			var scrollView = new ScrollView(ScrollViewMode.Vertical);
			var emptyContainer = new VisualElement { name = "listRoot" };

			var microserviceContentVisualElement = root.Q("microserviceContentVisualElement");
			microserviceContentVisualElement.Add(new VisualElement { name = "announcementList" });
			microserviceContentVisualElement.Add(scrollView);
			scrollView.Add(emptyContainer);
			OnLoad().Then(_ =>
			{
				_dataModel = Scope.GetService<UsamDataModel>();
				if (!_codeService.IsDockerRunning)
				{
					ShowDockerNotRunningAnnouncement();
					return;
				}

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
			_codeService.RefreshServices().Then(_ => Build());
		}

		public override async Promise OnLoad()
		{
			_codeService = Scope.GetService<CodeService>();


			await _codeService.OnReady;
		}
	}
}
