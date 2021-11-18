using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
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
			                                BUSSConstants.HierarchyWindowSize);
		}

		public BUSSHierarchyWindow() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BUSSHierarchyWindow)}/{nameof(BUSSHierarchyWindow)}") { }

		public override void Refresh()
		{
			base.Refresh();

			// IndentedLabelVisualElement label = new IndentedLabelVisualElement();
			// label.Setup("Test test");
			// label.Refresh();
			//
			// IndentedLabelVisualElement label1 = new IndentedLabelVisualElement();
			// label1.Setup("Test test", 1);
			// label1.Refresh();
			//
			// Root.Add(label);
			// Root.Add(label1);
			
			foreach (Object o in Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject obj = (GameObject) o;
				if (obj.transform.parent == null)
				{
					Text component = obj.GetComponent<Text>();

					if (component != null)
					{
						IndentedLabelVisualElement label = new IndentedLabelVisualElement();
						label.Setup(obj.name);
						label.Refresh();
						Root.Add(label);
					}

					Traverse(obj, 0);
				}
			}
		}
		
		
		void Traverse(GameObject obj, int level)
		{
			Text component = obj.GetComponent<Text>();

			if (component != null)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement();
				label.Setup(obj.name, level + 1);
				label.Refresh();
				Root.Add(label);
				
				foreach (Transform child in obj.transform)
				{
					Traverse(child.gameObject, level + 1);
				}
			}
			else
			{
				foreach (Transform child in obj.transform)
				{
					Traverse(child.gameObject, level);
				}
			}
		}
	}
}
