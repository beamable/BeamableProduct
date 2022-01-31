using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
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
		public event Action HierarchyChanged;
		public event Action<GameObject> SelectionChanged;

		private readonly List<IndentedLabelVisualElement> _spawnedLabels = new List<IndentedLabelVisualElement>();
		private ScrollView _hierarchyContainer;
		private IndentedLabelVisualElement _selectedLabel;

		public T SelectedComponent
		{
			get;
			private set;
		}

		private IndentedLabelVisualElement SelectedLabel
		{
			get => _selectedLabel;
			set
			{
				_selectedLabel = value;

				if (_selectedLabel == null)
				{
					return;
				}

				if (_selectedLabel.RelatedGameObject != null)
				{
					SelectionChanged?.Invoke(_selectedLabel.RelatedGameObject);
				}
			}
		}

		protected ComponentBasedHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement.uss")
		{ }

		public override void Init()
		{
			base.Init();
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

		protected abstract void OnObjectRegistered(T registeredObject);

		protected virtual string GetLabel(T component)
		{
			return component.name;
		}

		protected override void OnDestroy()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		protected virtual void OnHierarchyChanged()
		{
			foreach (IndentedLabelVisualElement child in _spawnedLabels)
			{
				child.Destroy();
			}

			_spawnedLabels.Clear();
			_hierarchyContainer.Clear();

			foreach (Object foundObject in Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject gameObject = (GameObject)foundObject;
				if (gameObject.transform.parent == null)
				{
					Traverse(gameObject, 0);
				}
			}

			HierarchyChanged?.Invoke();
		}

		protected virtual void OnSelectionChanged()
		{
			IndentedLabelVisualElement indentedLabelVisualElement =
				_spawnedLabels.Find(label => label.RelatedGameObject == Selection.activeGameObject);

			if (indentedLabelVisualElement != null)
			{
				SelectedComponent = indentedLabelVisualElement.RelatedGameObject.GetComponent<T>();
				ChangeSelectedLabel(indentedLabelVisualElement, false);
			}
		}

		private void ChangeSelectedLabel(IndentedLabelVisualElement newLabel, bool setInHierarchy = true)
		{
			SelectedLabel?.Deselect();
			SelectedLabel = newLabel;
			SelectedLabel?.Select();

			if (!setInHierarchy) return;

			Selection.SetActiveObjectWithContext(SelectedLabel?.RelatedGameObject,
												 SelectedLabel?.RelatedGameObject);
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
				label.Init();
				_spawnedLabels.Add(label);
				_hierarchyContainer.Add(label);

				OnObjectRegistered(foundComponent);

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
