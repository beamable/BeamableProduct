using Beamable;
using Beamable.Common;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Components;
using Beamable.Editor.UI;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using UnityEditor;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2
{
	public class UsamWindow : BeamEditorWindow<UsamWindow>
	{
		private CodeService _codeService;

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
			// _actionBarVisualElement.UpdateButtonsState(Model.AllLocalServices.Count(x => !x.IsArchived));

			_microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
			_microserviceBreadcrumbsVisualElement.Refresh();
			var ssa = root.Q("microserviceContentVisualElement");
			OnLoad().Then(_ =>
			{
				foreach (var info in _codeService.Services)
				{
					var el = new StandaloneMicroserviceVisualElement() { Info = info };
					ssa.Add(el);
					el.Refresh();
				}
			});
		}

		public override async Promise OnLoad()
		{
			_codeService = Scope.GetService<CodeService>();


			await _codeService.OnReady;
		}
	}
}