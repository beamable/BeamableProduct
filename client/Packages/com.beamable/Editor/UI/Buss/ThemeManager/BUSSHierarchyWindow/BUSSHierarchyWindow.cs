using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using UnityEditor;
using UnityEngine;

namespace Beamable.UI.BUSS
{
	public class BUSSHierarchyWindow : BeamableVisualElement
	{
#if BEAMABLE_DEVELOPER
		[MenuItem(
			BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
			BeamableConstants.OPEN + " " +
			BeamableConstants.BUSS_HIERARCHY_WINDOW,
			priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
#endif
		public static void Init()
		{
			BUSSHierarchyWindow window = new BUSSHierarchyWindow();
			BeamablePopupWindow.ShowUtility(BeamableConstants.BUSS_HIERARCHY_WINDOW, window, null,
			                                new Vector2(200, 200));
		}

		public BUSSHierarchyWindow() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BUSSHierarchyWindow)}/{nameof(BUSSHierarchyWindow)}") { }

		public override void Refresh()
		{
			base.Refresh();
		}
	}
}
