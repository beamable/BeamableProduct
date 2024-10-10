using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Microservice.UI;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.Microservice.UI2.PublishWindow;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
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
		private VisualElement _createServiceContainer;
		private ActionBarVisualElement _actionBarVisualElement;
		private MicroserviceBreadcrumbsVisualElement _microserviceBreadcrumbsVisualElement;
		private CreateServiceVisualElement _createServiceElement;

		private ScrollView _scrollView;
		private ServicesDisplayFilter _activeFilter = ServicesDisplayFilter.AllTypes;

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
			_actionBarVisualElement.OnSettingsButtonClicked += HandleSettingsButtonClicked;
			_microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
			_microserviceBreadcrumbsVisualElement.OnNewServicesDisplayFilterSelected += UpdateActiveFilter;
			_microserviceBreadcrumbsVisualElement.SetPreviousFilter(_activeFilter);
			_microserviceBreadcrumbsVisualElement.Refresh();
			var scrollViewParent = _windowRoot.Q<MicroserviceContentVisualElement>("microserviceContentVisualElement");
			scrollViewParent.style.flexGrow = 1;
			scrollViewParent.style.flexDirection = FlexDirection.Column;
			_scrollView = _windowRoot.Q<ScrollView>("microScrollView");

#if UNITY_2021_1_OR_NEWER
			_scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
#endif

			_scrollView.style.flexGrow = 1;
			_scrollView.style.overflow = Overflow.Hidden;

			var emptyContainer = new VisualElement { name = "listRoot" };

			_createServiceElement = new CreateServiceVisualElement();
			_createServiceElement.SetHidden(true);

			_createServiceContainer = _windowRoot.Q<VisualElement>("createServiceElement");
			_createServiceContainer.style.flexGrow = 0;
			_createServiceContainer.style.display = DisplayStyle.None;
			_createServiceContainer.Add(_createServiceElement);

			_scrollView.Add(emptyContainer);
			OnLoad().Then(_ =>
			{
				_dataModel = Scope.GetService<UsamDataModel>();
				var services = GetFilteredServicesDefinitions(_codeService, _activeFilter);

				foreach (IBeamoServiceDefinition beamoServiceDefinition in services)
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

		private static List<IBeamoServiceDefinition> GetFilteredServicesDefinitions(CodeService codeService, ServicesDisplayFilter filter)
		{
			var services = new List<IBeamoServiceDefinition>();

			switch (filter)
			{
				case ServicesDisplayFilter.AllTypes:
					services = codeService.ServiceDefinitions;
					break;
				case ServicesDisplayFilter.Microservices:
					services = codeService.ServiceDefinitions.Where(sd => sd.ServiceType == ServiceType.MicroService).ToList();
					break;
				case ServicesDisplayFilter.Storages:
					services = codeService.ServiceDefinitions.Where(sd => sd.ServiceType == ServiceType.StorageObject).ToList();
					break;
				case ServicesDisplayFilter.Archived:
					services = codeService.ServiceDefinitions.Where(sd => !sd.ShouldBeEnabledOnRemote).ToList();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
			}

			return services;
		}

		private void UpdateActiveFilter(ServicesDisplayFilter filter)
		{
			_activeFilter = filter;
			Build();
		}

		private void HandlePublishButtonClicked()
		{
			UsamPublishWindow.Init(BeamEditorContext.Default);
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

		private void HandleSettingsButtonClicked()
		{
			SettingsService.OpenProjectSettings($"Project/Beamable Services");
		}

		private void HandleCreateNewButtonClicked(ServiceType serviceType)
		{
			_createServiceElement.ServiceType = serviceType;
			_createServiceContainer.style.display = DisplayStyle.Flex;
			_createServiceElement.Refresh(() =>
			{
				_actionBarVisualElement.Refresh();
				_createServiceContainer.MarkDirtyRepaint();
				rootVisualElement.MarkDirtyRepaint();
			});
			_createServiceElement.OnClose += () =>
			{
				_createServiceContainer.style.display = DisplayStyle.None;
				_createServiceContainer.MarkDirtyRepaint();
				rootVisualElement.MarkDirtyRepaint();
			};
			_createServiceElement.OnCreateServiceFinished += () =>
			{
				_createServiceContainer.style.display = DisplayStyle.None;
				_createServiceContainer.MarkDirtyRepaint();
				rootVisualElement.MarkDirtyRepaint();
			};
			EditorApplication.delayCall += () => _scrollView.verticalScroller.value = 0f;
		}

		public override async Promise OnLoad()
		{
			_codeService = Scope.GetService<CodeService>();


			await _codeService.OnReady;
		}
	}
}
