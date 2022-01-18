using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using System.Collections.Generic;
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
	public abstract class ComponentBasedHierarchyVisualElement<T> : BeamableBasicVisualElement where T : MonoBehaviour
	{
#if UNITY_2018
		protected ComponentBasedHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public ComponentBasedHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement.uss") { }
#endif

		protected readonly List<IndentedLabelVisualElement> SpawnedLabels = new List<IndentedLabelVisualElement>();
		protected IndentedLabelVisualElement SelectedLabel
		{
			get;
			set;
		}
		
		private ScrollView _hierarchyContainer;

		public override void Refresh()
		{
			base.Refresh();
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

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			OnHierarchyChanged();
		}

		protected abstract void OnSelectionChanged();

		protected virtual string GetLabel(T component)
		{
			return component.name;
		}

		protected override void OnDestroy()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		protected void ChangeSelectedLabel(IndentedLabelVisualElement newLabel, bool setInHierarchy = true)
		{
			SelectedLabel?.Deselect();
			SelectedLabel = newLabel;
			SelectedLabel?.Select();
			
			if (!setInHierarchy) return;
			
			Selection.SetActiveObjectWithContext(SelectedLabel?.RelatedGameObject,
			                                     SelectedLabel?.RelatedGameObject);
		}

		private void OnHierarchyChanged()
		{
			foreach (IndentedLabelVisualElement child in SpawnedLabels)
			{
				child.Destroy();
			}

			SpawnedLabels.Clear();
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

		private void OnMouseClicked(IndentedLabelVisualElement newLabel)
		{
			ChangeSelectedLabel(newLabel);
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
				SpawnedLabels.Add(label);
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
