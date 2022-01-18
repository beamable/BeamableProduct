using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using Editor.UI.Buss;
using Editor.UI.BUSS;
using Editor.UI.BUSS.ThemeManager;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.UI.BUSS
{
	public class BussThemeManager : EditorWindow
	{
#if BEAMABLE_DEVELOPER
		// [MenuItem(
		// 	BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
		// 	BeamableConstants.OPEN + " " +
		// 	BeamableConstants.THEME_MANAGER,
		// 	priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		[MenuItem("Private/Theme Manager")]
#endif
		public static void Init()
		{
			BussThemeManager themeManagerWindow = GetWindow<BussThemeManager>(BeamableConstants.THEME_MANAGER, true);
			themeManagerWindow.Show(true);
		}

		private VisualElement _stylesGroup;
		private ObjectField _styleSheetSource;
		private BussElementHierarchyVisualElement _navigationWindow;

		private bool _inStyleSheetChangedLoop;
		private readonly VariableDatabase _variableDatabase = new VariableDatabase();
		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements = new List<BussStyleCardVisualElement>();

		private void OnEnable()
		{
			minSize = BUSSConstants.ThemeManagerWindowSize;
			Refresh();
		}

		private void Refresh()
		{
			VisualElement root = this.GetRootVisualContainer();
			root.Clear();

			VisualElement mainVisualElement = new VisualElement();
			mainVisualElement.name = "themeManagerContainer";

#if UNITY_2018			
			mainVisualElement.AddStyleSheet(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussThemeManager/BussThemeManager.2018.uss");
#elif UNITY_2019_1_OR_NEWER
			mainVisualElement.AddStyleSheet(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussThemeManager/BussThemeManager.uss");
#endif
			
			ScrollView scrollView = new ScrollView();
			scrollView.name = "themeManagerContainerScrollView";
			mainVisualElement.Add(scrollView);

			VisualElement navigationGroup = new VisualElement();
			navigationGroup.name = "navigationGroup";
			scrollView.Add(navigationGroup);
			
			_navigationWindow = new BussElementHierarchyVisualElement();
			_navigationWindow.Refresh();
			navigationGroup.Add(_navigationWindow);

			_stylesGroup = new VisualElement();
			_stylesGroup.name = "stylesGroup";
			scrollView.Add(_stylesGroup);

			root.Add(mainVisualElement);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;
			
			RefreshStyleSheets();
		}

		private void RefreshStyleSheets()
		{
			ClearCurrentStyleSheet();
			
			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				_variableDatabase.AddStyleSheet(styleSheet);
				styleSheet.Change += OnStyleSheetExternallyChanged;
			}

			RefreshStyleCards();
		}

		private void RefreshStyleCards()
		{
			ClearStyleCards();
			CreateStyleCards();
			AddSelectorButton();
		}
		
		private void AddSelectorButton()
		{
			var addSelectorButton = new VisualElement {name = "addSelectorButton"};
			addSelectorButton.AddToClassList("button");
			addSelectorButton.Add(new Label("Add Selector"));
			addSelectorButton.UnregisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());
			addSelectorButton.RegisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());

			EditorApplication.update -= CheckEnableState;
			EditorApplication.update += CheckEnableState;
			
			_stylesGroup.Add(addSelectorButton);

			void OpenAddSelectorWindow()
			{
				var window = AddSelectorWindow.ShowWindow();
				window?.Init(_ => RefreshStyleSheets());
			}
			
			void CheckEnableState()
			{
				addSelectorButton.tooltip = string.Empty;
				var styleSheets = Helper.FindAssets<BussStyleSheet>("t:BussStyleSheet", new[] {"Assets"});
				if (styleSheets.Count == 0)
				{
					addSelectorButton.tooltip = "There should be created at least one BUSS Style Config!";
					addSelectorButton.SetEnabled(false);
				}
				else 
					addSelectorButton.SetEnabled(true);
			}
		}

		private void ClearCurrentStyleSheet()
		{
			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				styleSheet.Change -= OnStyleSheetExternallyChanged;
			}

			_variableDatabase.RemoveAllStyleSheets();
		}

		private void ClearStyleCards()
		{
			foreach (BussStyleCardVisualElement styleCard in _styleCardsVisualElements)
			{
				styleCard.Destroy();
			}
			
			_styleCardsVisualElements.Clear();
			_stylesGroup.Clear();
		}

		private void CreateStyleCards()
		{
			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				foreach (BussStyleRule styleRule in styleSheet.Styles)
				{
					BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
					styleCard.Setup(styleSheet, styleRule, _variableDatabase, _navigationWindow);
					_styleCardsVisualElements.Add(styleCard);
					_stylesGroup.Add(styleCard);
				}
			}
		}

		private void OnStyleSheetExternallyChanged()
		{
			if(_inStyleSheetChangedLoop) return;
			
			_inStyleSheetChangedLoop = true;
			
			_variableDatabase.ReconsiderAllStyleSheets();
			
			// TODO: We will use it in order to update only visual elements affected by change.
			// foreach (BussPropertyVisualElement propertyVisualElement in this.GetRootVisualContainer().Query<BussPropertyVisualElement>().Build().ToList())
			// {
			// 	propertyVisualElement.OnPropertyChangedExternally();
			// }
			
			RefreshStyleCards();
			_inStyleSheetChangedLoop = false;
		}

		private void OnDestroy()
		{
			ClearCurrentStyleSheet();
			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.Destroy();
		}
	}
}
