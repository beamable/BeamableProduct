#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif
using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
		private ThemeManagerNavigationComponent _navigationWindow;
		private ScrollView _scrollView;
		private SelectedBussElementVisualElement _selectedBussElement;
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
			_selectedBussElement?.Destroy();

			UndoSystem<BussStyleRule>.DeleteAllRecords();

			// _model.OnChange -= Refresh;

			// if (_navigationWindow != null)
			// {
			// _navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			// _navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			// _navigationWindow.SelectionChanged -= SetScroll;
			// }
		}

		private void OnFocus()
		{
			_model.OnFocus();
			// _navigationWindow?.ForceRebuild();
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
				new BussThemeManagerActionBarVisualElement(OnAddStyleButtonClicked, OnCopyButtonClicked,
				                                           Refresh, OnDocsButtonClicked, OnSearch) {name = "actionBar"};

			actionBar.Init();
			mainVisualElement.Add(actionBar);

			VisualElement navigationGroup = new VisualElement {name = "navigationGroup"};
			mainVisualElement.Add(navigationGroup);

			_navigationWindow = new ThemeManagerNavigationComponent(_model);
			_navigationWindow.Init();
			navigationGroup.Add(_navigationWindow);

			_selectedBussElement = new SelectedBussElementVisualElement(_model);
			_selectedBussElement.Init();
			mainVisualElement.Add(_selectedBussElement);

			_scrollView = new ScrollView {name = "themeManagerContainerScrollView"};
			_stylesGroup = new BussStyleListVisualElement(_model) {name = "stylesGroup"};
			_stylesGroup.Init();
			_scrollView.Add(_stylesGroup);
			
			// TODO: remove constructor params after moving variables and properties database into model
			InlineStyleCardVisualElement inlineStyle =
				new InlineStyleCardVisualElement(_stylesGroup.VariableDatabase, _stylesGroup.PropertyDatabase);
			inlineStyle.Init();
			mainVisualElement.Add(inlineStyle);
			
			mainVisualElement.Add(_scrollView);

			// _navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			// _navigationWindow.HierarchyChanged += RefreshStyleSheets;

			// _navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			// _navigationWindow.BussStyleSheetChange += RefreshStyleSheets;

			// _navigationWindow.SelectionChanged -= SetScroll;
			// _navigationWindow.SelectionChanged += SetScroll;

			root.Add(_windowRoot);

			// _model.OnChange += Refresh;

			//RefreshStyleSheets();
		}

		private void Refresh()
		{
			//_stylesGroup.StyleSheets = _navigationWindow.StyleSheets;
		}

		// private void SetScroll(GameObject _ = null)
		// {
		// 	EditorApplication.delayCall += () => UpdateScroll(_stylesGroup.GetSelectedElementPosInScroll());
		// }

		private void UpdateScroll(float scrollValue)
		{
			EditorApplication.delayCall += () =>
			{
				_scrollView.verticalScroller.value = scrollValue;
				_scrollView.MarkDirtyRepaint();
			};
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
				CreateEmptyStyle(_stylesGroup.WritableStyleSheets.First());
			}
			else if (styleSheetCount > 1)
			{
				OpenAddStyleMenu(_stylesGroup.WritableStyleSheets);
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
					CreateEmptyStyle(styleSheet);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet, string selectorName = "*")
		{
			if (Selection.activeGameObject != null && _selectedBussElement != null)
			{
				// TODO: get this selector name from selected buss element from model
				// selectorName = _navigationWindow.SelectedElementLabel();
			}

			BussStyleRule selector = BussStyleRule.Create(selectorName, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			Refresh();
			AssetDatabase.SaveAssets();
		}

		private void OnCopyButtonClicked()
		{
			// List<BussStyleSheet> readonlyStyles =
			// 	_stylesGroup.StyleSheets.Where(styleSheet => styleSheet.IsReadOnly).ToList();
			// OpenCopyMenu(readonlyStyles);
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

		private void OnDocsButtonClicked()
		{
			Application.OpenURL(URLs.Documentations.URL_DOC_BUSS_THEME_MANAGER);
		}

		private void OnSearch(string value)
		{
			_stylesGroup.SetFilter(value);
		}

		#endregion
	}
}
