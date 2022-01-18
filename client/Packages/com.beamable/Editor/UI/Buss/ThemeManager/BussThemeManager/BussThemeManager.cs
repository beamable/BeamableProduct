using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using Editor.UI.BUSS;
using Editor.UI.BUSS.ThemeManager;
using System.Collections.Generic;
using UnityEditor;
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
		private BussStyleSheet _currentStyleSheet;
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

			_styleSheetSource = new ObjectField();
			_styleSheetSource.allowSceneObjects = false;
			_styleSheetSource.name = "styleSheetSource";
			_styleSheetSource.objectType = typeof(BussStyleSheet);
			_styleSheetSource.UnregisterValueChangedCallback(StyleSheetChanged);
			_styleSheetSource.RegisterValueChangedCallback(StyleSheetChanged);
			scrollView.Add(_styleSheetSource);

			_stylesGroup = new VisualElement();
			_stylesGroup.name = "stylesGroup";
			scrollView.Add(_stylesGroup);
			
			root.Add(mainVisualElement);
		}

		private void StyleSheetChanged(ChangeEvent<Object> evt)
		{
			ClearCurrentStyleSheet();
			_currentStyleSheet = (BussStyleSheet)evt.newValue;

			if (_currentStyleSheet == null) return;
			
			_variableDatabase.AddStyleSheet(_currentStyleSheet);

			_currentStyleSheet.Change += OnStyleSheetExternallyChanged;

			RefreshStyleCards();
		}

		private void RefreshStyleCards()
		{
			ClearStyleCards();
			CreateStyleCards();
		}

		private void ClearCurrentStyleSheet()
		{
			if (_currentStyleSheet != null)
			{
				_currentStyleSheet.Change -= OnStyleSheetExternallyChanged;
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
			foreach (BussStyleRule styleRule in _currentStyleSheet.Styles)
			{
				BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
				styleCard.Setup(_currentStyleSheet, styleRule, _variableDatabase, _navigationWindow);
				_styleCardsVisualElements.Add(styleCard);
				_stylesGroup.Add(styleCard);
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
			_navigationWindow.Destroy();
		}
	}
}
