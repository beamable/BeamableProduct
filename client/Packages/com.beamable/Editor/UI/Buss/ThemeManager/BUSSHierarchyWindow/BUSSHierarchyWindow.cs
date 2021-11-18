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

		private IndentedLabelVisualElement _selectedComponent;
		private ScrollView _container;

		private BUSSHierarchyWindow() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BUSSHierarchyWindow)}/{nameof(BUSSHierarchyWindow)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_container = Root.Q<ScrollView>("scrollView");
			
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			
			OnHierarchyChanged();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		private void OnHierarchyChanged()
		{
			_container.Clear();

			foreach (Object foundObject in Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject gameObject = (GameObject) foundObject;
				if (gameObject.transform.parent == null)
				{
					Traverse(gameObject, 0);
				}
			}
		}

		private void OnMouseClicked(IndentedLabelVisualElement clickedComponent)
		{
			_selectedComponent?.Deselect();
			_selectedComponent = clickedComponent;
			_selectedComponent?.Select();
			Selection.SetActiveObjectWithContext(_selectedComponent?.RelatedBussElement, _selectedComponent?.RelatedBussElement);
		}

		private void Traverse(GameObject gameObject, int currentLevel)
		{
			// TODO: this is temporary component class. It should be removed after buss-system branch will be merged into main
			// BUSSElement component will be available
			Text foundComponent = gameObject.GetComponent<Text>();

			if (foundComponent != null)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement(); 
				_container.Add(label);
				label.Setup(foundComponent, OnMouseClicked, currentLevel);
				label.Refresh();
				
				foreach (Transform child in gameObject.transform)
				{
					Traverse(child.gameObject, currentLevel + 1);
				}
			}
			else
			{
				foreach (Transform child in gameObject.transform)
				{
					Traverse(child.gameObject, currentLevel);
				}
			}
		}
	}

	// TODO: this is temporary class. It should be removed after buss-system branch will be merged into main
	public class BUSSElement : MonoBehaviour
	{
		public string Name
		{
			get;
		}
	}
}
