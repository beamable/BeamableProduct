using Beamable.Editor.UI.Buss;
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
	public class ComponentBasedHierarchyVisualElement<T> : BeamableVisualElement where T : MonoBehaviour
	{
		private IndentedLabelVisualElement _selectedComponent;
		private ScrollView _container;

		protected ComponentBasedHierarchyVisualElement(string commonPath) : base(commonPath) { }

		public override void Refresh()
		{
			base.Refresh();

			_container = Root.Q<ScrollView>("scrollView");

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;

			OnHierarchyChanged();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		protected virtual string GetLabel(T component)
		{
			return component.name;
		}

		private void OnHierarchyChanged()
		{
			_container.Clear();

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
				_container.Add(label);

				label.Setup(foundComponent.gameObject, GetLabel(foundComponent), OnMouseClicked,
				            currentLevel, IndentedLabelVisualElement.DEFAULT_SINGLE_INDENT_WIDTH);
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
}
