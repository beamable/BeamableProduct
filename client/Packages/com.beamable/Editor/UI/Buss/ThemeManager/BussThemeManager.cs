#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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

		private BeamablePopupWindow _confirmationPopup;
		private AddStyleButton _addStyleButton;
		private GameObject _selectedGameObject;
		private bool _inStyleSheetChangedLoop;
		private bool _filterMode;

		static BussThemeManager()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig
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

		protected override void Build()
		{
			minSize = THEME_MANAGER_WINDOW_SIZE;

			VisualElement root = this.GetRootVisualContainer();
			root.Clear();

			VisualTreeAsset uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();
			_addStyleButton = null;

			VisualElement mainVisualElement = _windowRoot.Q("window-main");

			mainVisualElement.AddStyleSheet($"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");
			mainVisualElement.TryAddScrollViewAsMainElement();

			BussThemeManagerActionBarVisualElement actionBar = new BussThemeManagerActionBarVisualElement(OnAddStyleButtonClicked, OnCopyButtonClicked);
			actionBar.name = "actionBar";
			actionBar.Init();
			mainVisualElement.Add(actionBar);

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

			InlineStyleCardVisualElement inlineStyle =
				new InlineStyleCardVisualElement(_stylesGroup.VariableDatabase, _stylesGroup.PropertyDatabase);
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
			GameObject selected = Selection.activeGameObject;
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
		
		#region Action bar buttons' actions
		private void OnAddStyleButtonClicked()
		{
			int styleSheetCount = _stylesGroup.WritableStyleSheets.Count();

			if (styleSheetCount == 0)
			{
				return;
			}

			if (styleSheetCount == 1)
			{
				CreateEmptyStyle(_stylesGroup.WritableStyleSheets.First(), Features.Buss.NEW_SELECTOR_NAME);
			}
			else if (styleSheetCount > 1)
			{
				OpenAddStyleMenu(_stylesGroup.WritableStyleSheets);
			}
		}

		public void CheckEnableState(MouseEnterEvent evt = null)
		{
			if (_addStyleButton == null) return;

			_addStyleButton.tooltip = string.Empty;

			int styleSheetCount = _stylesGroup.WritableStyleSheets?.Count() ?? 0;

			if (styleSheetCount == 0)
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
			}
			else
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);
			}
		}

		private void OpenAddStyleMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(ADD_STYLE_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					CreateEmptyStyle(styleSheet, Features.Buss.NEW_SELECTOR_NAME);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet, string newName = "")
		{
			BussStyleRule selector = BussStyleRule.Create(newName, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			RefreshStyleSheets();
			AssetDatabase.SaveAssets();
		}
		
		private void OnCopyButtonClicked()
		{
			List<BussStyleSheet> readonlyStyles =
				_stylesGroup.StyleSheets.Where(styleSheet => styleSheet.IsReadOnly).ToList();
			OpenCopyMenu(readonlyStyles);
		}

		private void OpenCopyMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(DUPLICATE_STYLESHEET_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					NewStyleSheetWindow window = NewStyleSheetWindow.ShowWindow();
					if (window != null)
					{
						window.Init(styleSheet.Styles);
					}
				});
			}

			context.ShowAsContext();
		}
		#endregion
	}
}
