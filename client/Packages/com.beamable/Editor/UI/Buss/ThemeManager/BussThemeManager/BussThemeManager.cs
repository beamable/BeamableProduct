using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using Editor.UI.BUSS;
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
			
			// ScrollView scrollView = new ScrollView();
			// scrollView.name = "scrollView";

			VisualElement navigationGroup = new VisualElement();
			navigationGroup.name = "navigationGroup";
			mainVisualElement.Add(navigationGroup);
			
			BussElementHierarchyVisualElement hierarchyComponent = new BussElementHierarchyVisualElement();
			navigationGroup.Add(hierarchyComponent);
			hierarchyComponent.Refresh();

			_styleSheetSource = new ObjectField();
			_styleSheetSource.allowSceneObjects = false;
			_styleSheetSource.name = "styleSheetSource";
			_styleSheetSource.objectType = typeof(BussStyleSheet);
			_styleSheetSource.UnregisterValueChangedCallback(StyleSheetChanged);
			_styleSheetSource.RegisterValueChangedCallback(StyleSheetChanged);
			mainVisualElement.Add(_styleSheetSource);

			_stylesGroup = new VisualElement();
			_stylesGroup.name = "stylesGroup";
			mainVisualElement.Add(_stylesGroup);

			// mainVisualElement.Add(scrollView);
			
			root.Add(mainVisualElement);
		}

		private void StyleSheetChanged(ChangeEvent<Object> evt)
		{
			ClearCurrentStyleSheet();
			_currentStyleSheet = (BussStyleSheet)evt.newValue;

			if (_currentStyleSheet == null)
			{
				return;
			}

			foreach (BussStyleRule styleRule in _currentStyleSheet.Styles)
			{
				BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
				styleCard.Setup(styleRule);
				_stylesGroup.Add(styleCard);
			}
		}

		private void ClearCurrentStyleSheet()
		{
			_stylesGroup.Clear();
		}
	}
}
