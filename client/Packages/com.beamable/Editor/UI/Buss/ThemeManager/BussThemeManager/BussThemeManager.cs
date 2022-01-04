using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using Editor.UI.BUSS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.UI.BUSS
{
	public class BussThemeManager : BeamableVisualElement
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
			BussThemeManager window = new BussThemeManager();
			BeamablePopupWindow.ShowUtility(BeamableConstants.THEME_MANAGER, window, null,
			                                BUSSConstants.ThemeManagerWindowSize);
		}

		private VisualElement _navigationGroup;
		private VisualElement _stylesGroup;
		private ObjectField _styleSheetSource;
		private BussStyleSheet _currentStyleSheet;

		private BussThemeManager() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussThemeManager)}/{nameof(BussThemeManager)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_navigationGroup = Root.Q<VisualElement>("navigation");
			_stylesGroup = Root.Q<VisualElement>("styles");
			_styleSheetSource = Root.Q<ObjectField>("styleSheetSource");
			_styleSheetSource.objectType = typeof(BussStyleSheet);
			_styleSheetSource.UnregisterValueChangedCallback(StyleSheetChanged);
			_styleSheetSource.RegisterValueChangedCallback(StyleSheetChanged);

			BussElementHierarchyVisualElement hierarchyComponent = new BussElementHierarchyVisualElement();

			hierarchyComponent.Refresh();
			_navigationGroup.Add(hierarchyComponent);
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
