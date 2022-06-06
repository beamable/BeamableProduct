// unset

using Beamable.Common;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Environment
{
	public class BeamableEnvironmentOverridesWindow : EditorWindow
	{
		[MenuItem(Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES +
		          "/Environment Overrides")]
		private static void Open()
		{
			var window = CreateInstance<BeamableEnvironmentOverridesWindow>();
			window.titleContent = new GUIContent("Override Beamable Environment");
			window.Show();
		}
		
		private void CreateGUI()
		{
			var element = new BeamableEnvironmentOverridesVisualElement();
			rootVisualElement.Add(element);
			element.Init();
		}
	}
}
