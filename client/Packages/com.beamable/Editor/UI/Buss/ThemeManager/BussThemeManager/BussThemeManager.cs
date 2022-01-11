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
		private BussElementHierarchyVisualElement _hierarchyComponent;

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
			
			_hierarchyComponent = new BussElementHierarchyVisualElement();
			_hierarchyComponent.Refresh();
			navigationGroup.Add(_hierarchyComponent);

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

			if (_currentStyleSheet == null)
			{
				return;
			}

			_currentStyleSheet.OnChange += OnStyleSheetExternallyChanged;

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
			if (_currentStyleSheet != null)
			{
				_currentStyleSheet.OnChange -= OnStyleSheetExternallyChanged;
			}
		}

		private void OnStyleSheetExternallyChanged()
		{
			foreach (BussPropertyVisualElement propertyVisualElement in this.GetRootVisualContainer().Query<BussPropertyVisualElement>().Build().ToList())
			{
				propertyVisualElement.OnPropertyChangedExternally();
			}
		}

		private void OnDestroy()
		{
			ClearCurrentStyleSheet();
			_hierarchyComponent.Destroy();
		}
	}
}
