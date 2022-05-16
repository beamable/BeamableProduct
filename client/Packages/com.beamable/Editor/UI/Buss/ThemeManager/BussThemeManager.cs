using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
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
	public class BussThemeManager : EditorWindow
	{
		private BussStyleListVisualElement _stylesGroup;
		private BussElementHierarchyVisualElement _navigationWindow;
		private LabeledCheckboxVisualElement _filterToggle;
		private ScrollView _scrollView;
		private SelectedBussElementVisualElement _selectedBussElement;

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		private BeamablePopupWindow _confirmationPopup;
		private AddStyleButton _addStyleButton;
		private GameObject _selectedGameObject;
		private bool _inStyleSheetChangedLoop;
		private bool _filterMode;

		[MenuItem(
			MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " " +
			MenuItems.Windows.Names.THEME_MANAGER,
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2 + 5)]
		public static void Init()
		{
			Type inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			BussThemeManager themeManagerWindow = GetWindow<BussThemeManager>(MenuItems.Windows.Names.THEME_MANAGER, true, inspector);
			themeManagerWindow.Show(true);
		}

		private void OnEnable()
		{
			minSize = THEME_MANAGER_WINDOW_SIZE;
			Refresh();
		}

		private void Refresh()
		{
			VisualElement root = this.GetRootVisualContainer();
			root.Clear();
			_styleCardsVisualElements.Clear();
			_addStyleButton = null;
			VisualElement mainVisualElement = new VisualElement();
			mainVisualElement.name = "themeManagerContainer";

			mainVisualElement.AddStyleSheet(
				$"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");

			VisualElement navigationGroup = new VisualElement();
			navigationGroup.name = "navigationGroup";
			mainVisualElement.Add(navigationGroup);

			_navigationWindow = new BussElementHierarchyVisualElement();
			_navigationWindow.Init();
			navigationGroup.Add(_navigationWindow);

			_filterToggle = new LabeledCheckboxVisualElement("Filter by selected element");
			_filterToggle.name = "filterToggle";
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

			root.Add(mainVisualElement);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;

			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange += RefreshStyleSheets;

			_navigationWindow.SelectionChanged -= SetScroll;
			_navigationWindow.SelectionChanged += SetScroll;

			_navigationWindow.SelectionChanged -= CacheSelectedGameObject;
			_navigationWindow.SelectionChanged += CacheSelectedGameObject;

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
			_addStyleButton.CheckEnableState();
		}

		private void OnDestroy()
		{
			_filterToggle.OnValueChanged -= OnFilterToggleClicked;

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.SelectionChanged -= SetScroll;

			_navigationWindow.Destroy();
			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}
	}
}
