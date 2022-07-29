using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussThemeManager : BeamEditorWindow<BussThemeManager>
	{
		private BussStyleListVisualElement _stylesGroup;
		private BussElementHierarchyVisualElement _navigationWindow;
		private LabeledCheckboxVisualElement _filterToggle;
		private LabeledCheckboxVisualElement _hideOverridenToggle;
		private ScrollView _scrollView;
		private SelectedBussElementVisualElement _selectedBussElement;
		private VisualElement _windowRoot;

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		private BeamablePopupWindow _confirmationPopup;
		private AddStyleButton _addStyleButton;
		private GameObject _selectedGameObject;
		private bool _inStyleSheetChangedLoop;
		private bool _filterMode;

		static BussThemeManager()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = MenuItems.Windows.Names.THEME_MANAGER,
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = false,
			};
		}

		[MenuItem(
			MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " " +
			MenuItems.Windows.Names.THEME_MANAGER,
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2 + 5)]

		public static async void Init() => await GetFullyInitializedWindow();
		public static async void Init(BeamEditorWindowInitConfig initParameters) => await GetFullyInitializedWindow(initParameters);

		protected override void Build()
		{
			minSize = THEME_MANAGER_WINDOW_SIZE;

			var root = this.GetRootVisualContainer();
			root.Clear();

			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();
			_styleCardsVisualElements.Clear();
			_addStyleButton = null;

			var mainVisualElement = _windowRoot.Q("window-main");

			mainVisualElement.AddStyleSheet(
				$"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");
			mainVisualElement.TryAddScrollViewAsMainElement();

			VisualElement navigationGroup = new VisualElement();
			navigationGroup.name = "navigationGroup";
			mainVisualElement.Add(navigationGroup);

			_navigationWindow = new BussElementHierarchyVisualElement();
			_navigationWindow.Init();
			navigationGroup.Add(_navigationWindow);

			_filterToggle = new LabeledCheckboxVisualElement("Filter by selected element");
			_filterToggle.name = "toggle";
			_filterToggle.OnValueChanged -= OnFilterToggleClicked;
			_filterToggle.OnValueChanged += OnFilterToggleClicked;
			_filterToggle.Refresh();
			_filterToggle.SetWithoutNotify(_filterMode);
			mainVisualElement.Add(_filterToggle);
			
			_selectedBussElement = new SelectedBussElementVisualElement();
			_selectedBussElement.Setup(_navigationWindow);
			mainVisualElement.Add(_selectedBussElement);

			_stylesGroup = new BussStyleListVisualElement();

			var inlineStyle = new InlineStyleCardVisualElement(_stylesGroup.VariableDatabase, _stylesGroup.PropertyDatabase);
			mainVisualElement.Add(inlineStyle);
			inlineStyle.Init();

			_scrollView = new ScrollView();
			_scrollView.name = "themeManagerContainerScrollView";
			mainVisualElement.Add(_scrollView);
			_stylesGroup.name = "stylesGroup";
			_stylesGroup.Filter = CardFilter;
			_scrollView.Add(_stylesGroup);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;

			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange += RefreshStyleSheets;

			_navigationWindow.SelectionChanged -= SetScroll;
			_navigationWindow.SelectionChanged += SetScroll;

			_navigationWindow.SelectionChanged -= CacheSelectedGameObject;
			_navigationWindow.SelectionChanged += CacheSelectedGameObject;

			root.Add(_windowRoot);

			RefreshStyleSheets();
			AddSelectorButton(mainVisualElement);
		}

		private void CacheSelectedGameObject(GameObject go)
		{
			_selectedGameObject = go;
		}

		private void OnFilterToggleClicked(bool value)
		{
			_filterMode = value;
			_stylesGroup.FilterCards();
		}

		private bool CardFilter(BussStyleSheet styleSheet, BussStyleRule styleRule)
		{
			var selected = Selection.activeGameObject;
			BussElement selectedElement = null;
			if (selected != null)
			{
				selectedElement = selected.GetComponent<BussElement>();
			}

			if (selectedElement == null || !_filterMode) return true;

			return styleRule.Selector?.CheckMatch(_navigationWindow.SelectedComponent) ?? false;
		}

		private void RefreshStyleSheets()
		{
			_stylesGroup.StyleSheets = _navigationWindow.StyleSheets;
		}

		private void AddSelectorButton(VisualElement parent)
		{
			_addStyleButton = new AddStyleButton();
			_addStyleButton.Setup(_stylesGroup, _ => RefreshStyleSheets());
			_addStyleButton.CheckEnableState();
			parent.Insert(parent.Children().Count() - 1, _addStyleButton);
		}

		private void SetScroll(GameObject _ = null)
		{
			if (!_filterMode)
			{
				EditorApplication.delayCall += () => UpdateScroll(_stylesGroup.GetSelectedElementPosInScroll());
			}
		}

		private void UpdateScroll(float scrollValue)
		{
			EditorApplication.delayCall += () =>
			{
				_scrollView.verticalScroller.value = scrollValue;
				_scrollView.MarkDirtyRepaint();
			};
		}

		private void OnFocus()
		{
			_navigationWindow?.ForceRebuild(_selectedGameObject);
			_addStyleButton?.CheckEnableState();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();

			if (_filterToggle != null)
			{
				_filterToggle.OnValueChanged -= OnFilterToggleClicked;
			}

			if (_navigationWindow != null)
			{
				_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
				_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
				_navigationWindow.SelectionChanged -= SetScroll;
				_navigationWindow.Destroy();
			}

			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}
	}
}
