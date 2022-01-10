using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class ComponentBasedHierarchyVisualElement<T> : BeamableBasicVisualElement where T : MonoBehaviour
	{
#if UNITY_2018
		protected ComponentBasedHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public ComponentBasedHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement.uss") { }
#endif

		private ScrollView _hierarchyContainer;
		private IndentedLabelVisualElement _selectedComponent;

		public override void Refresh()
		{
			base.Refresh();
			// Root = new VisualElement().WithName("mainContainer");
			VisualElement header = new VisualElement();
			header.name = "header";
			TextElement label = new TextElement();
			label.name = "headerLabel";
			label.text = "Navigation window";
			header.Add(label);
			Root.Add(header);

			_hierarchyContainer = new ScrollView();
			_hierarchyContainer.name = "elementsContainer";
			Root.Add(_hierarchyContainer);

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;

			OnHierarchyChanged();
		}

		protected override void OnDestroy()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		protected virtual string GetLabel(T component)
		{
			return component.name;
		}

		private void OnHierarchyChanged()
		{
			foreach (VisualElement visualElement in _hierarchyContainer.Children())
			{
				// TODO: check this!!!
				BeamableBasicVisualElement element = visualElement as BeamableBasicVisualElement;
				element?.Destroy();
			}

			_hierarchyContainer.Clear();

			foreach (Object foundObject in Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject gameObject = (GameObject)foundObject;
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
			Selection.SetActiveObjectWithContext(_selectedComponent?.RelatedGameObject,
			                                     _selectedComponent?.RelatedGameObject);
		}

		private void Traverse(GameObject gameObject, int currentLevel)
		{
			T foundComponent = gameObject.GetComponent<T>();

			if (foundComponent != null)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement();
				label.Setup(foundComponent.gameObject, GetLabel(foundComponent), OnMouseClicked,
				            currentLevel, IndentedLabelVisualElement.DEFAULT_SINGLE_INDENT_WIDTH);
				label.Refresh();
				_hierarchyContainer.Add(label);

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
}
