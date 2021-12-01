using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Editor.UI.BUSS;
using UnityEditor;
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
		[MenuItem(
			BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
			BeamableConstants.OPEN + " " +
			BeamableConstants.THEME_MANAGER,
			priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
#endif
		public static void Init()
		{
			BussThemeManager window = new BussThemeManager();
			BeamablePopupWindow.ShowUtility(BeamableConstants.THEME_MANAGER, window, null,
				BUSSConstants.ThemeManagerWindowSize);
		}

		private VisualElement _navigationGroup;
		private VisualElement _stylesGroup;

		private BussThemeManager() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussThemeManager)}/{nameof(BussThemeManager)}")
		{ }

		public override void Refresh()
		{
			base.Refresh();

			_navigationGroup = Root.Q<VisualElement>("navigation");
			_stylesGroup = Root.Q<VisualElement>("styles");
			
			ComponentBasedHierarchyVisualElement<Text> hierarchyComponent = new ComponentBasedHierarchyVisualElement<Text>();
			hierarchyComponent.Refresh();
			_navigationGroup.Add(hierarchyComponent);

			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Refresh();
			_stylesGroup.Add(styleCard);
		}
	}
}
