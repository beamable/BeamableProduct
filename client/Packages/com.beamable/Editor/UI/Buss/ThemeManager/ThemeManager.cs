#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif
using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using UnityEditor;
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	// TODO: TD000003
	public class ThemeManager : BeamEditorWindow<ThemeManager>
	{
		private BeamablePopupWindow _confirmationPopup;
		private LabeledCheckboxVisualElement _filterToggle;
		private LabeledCheckboxVisualElement _hideOverridenToggle;
		private bool _inStyleSheetChangedLoop;
		private NavigationVisualElement _navigationWindow;
		private ScrollView _scrollView;
		private SelectedElementVisualElement _selectedElement;
		private BussStyleListVisualElement _stylesGroup;
		private VisualElement _windowRoot;
		private ThemeManagerModel _model;

		static ThemeManager()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig
			{
				Title = MenuItems.Windows.Names.THEME_MANAGER,
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = false,
			};
		}

		public override void OnDestroy()
		{
			base.OnDestroy();

			_navigationWindow?.Destroy();
			_selectedElement?.Destroy();
			_model?.Clear();

			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}

		private void OnFocus()
		{
			_model.OnFocus();
		}

		[MenuItem(
			MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " " +
			MenuItems.Windows.Names.THEME_MANAGER,
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2 + 5)]
		public static async void Init() => await GetFullyInitializedWindow();

		protected override void Build()
		{
			_model = new ThemeManagerModel();

			minSize = THEME_MANAGER_WINDOW_SIZE;

			VisualElement root = this.GetRootVisualContainer();
			root.Clear();

			VisualTreeAsset uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BUSS_THEME_MANAGER_PATH}/ThemeManager.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BUSS_THEME_MANAGER_PATH}/ThemeManager.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();

			VisualElement mainVisualElement = _windowRoot.Q("window-main");

			mainVisualElement.AddStyleSheet($"{BUSS_THEME_MANAGER_PATH}/ThemeManager.uss");
			mainVisualElement.TryAddScrollViewAsMainElement();

			BussThemeManagerActionBarVisualElement actionBar =
				new BussThemeManagerActionBarVisualElement(_model.OnAddStyleButtonClicked, _model.OnCopyButtonClicked,
				                                           _model.ForceRefresh, _model.OnDocsButtonClicked,
				                                           _model.OnSearch) {name = "actionBar"};

			actionBar.Init();
			mainVisualElement.Add(actionBar);

			VisualElement navigationGroup = new VisualElement {name = "navigationGroup"};
			mainVisualElement.Add(navigationGroup);

			_navigationWindow = new NavigationVisualElement(_model);
			_navigationWindow.Init();
			navigationGroup.Add(_navigationWindow);

			_selectedElement = new SelectedElementVisualElement(_model);
			_selectedElement.Init();
			mainVisualElement.Add(_selectedElement);

			_scrollView = new ScrollView {name = "themeManagerContainerScrollView"};
			_stylesGroup = new BussStyleListVisualElement(_model) {name = "stylesGroup"};
			_stylesGroup.Init();
			_scrollView.Add(_stylesGroup);

			InlineStyleVisualElement inlineStyle = new InlineStyleVisualElement(_model);
			inlineStyle.Init();
			mainVisualElement.Add(inlineStyle);
			mainVisualElement.Add(_scrollView);
			root.Add(_windowRoot);
		}
	}
}
