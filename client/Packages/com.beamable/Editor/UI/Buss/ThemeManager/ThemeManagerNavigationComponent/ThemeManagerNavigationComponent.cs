#region

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;
using Object = UnityEngine.Object;

#endregion

namespace Beamable.Editor.UI.Components
{
	public class ThemeManagerNavigationComponent : BeamableBasicVisualElement
	{
		public event Action<GameObject> SelectionChanged;
		public event Action BussStyleSheetChange;
		public event Action HierarchyChanged;
		public event Action SelectionCleared;

		private readonly List<IndentedLabelVisualElement> _spawnedLabels = new List<IndentedLabelVisualElement>();
		private bool _hasDelayedChangeCallback;
		private ScrollView _hierarchyContainer;
		private IndentedLabelVisualElement _selectedLabel;

		public List<BussStyleSheet> StyleSheets { get; } = new List<BussStyleSheet>();

		private IEnumerable<BussElement> Components =>
			_spawnedLabels.Select(l => l.RelatedGameObject.GetComponent<BussElement>());

		private BussElement SelectedComponent { get; set; }

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

		public ThemeManagerNavigationComponent() : base(
			$"{BUSS_THEME_MANAGER_PATH}/ThemeManagerNavigationComponent/ThemeManagerNavigationComponent.uss") { }

		public void ForceRebuild()
		{
			StyleSheets.Clear();
			RefreshTree();
			OnBussStyleSheetChange();

			if (Selection.activeGameObject != null)
			{
				OnSelectionChanged();
			}
		}

		public override void Init()
		{
			base.Init();
			VisualElement header = new VisualElement {name = "header"};
			TextElement label = new TextElement {name = "headerLabel", text = "Navigation"};
			header.Add(label);

			header.RegisterCallback<MouseDownEvent>(evt =>
			{
				_hierarchyContainer.ToggleInClassList("hidden");
			});

			Root.Add(header);

			_hierarchyContainer = new ScrollView {name = "elementsContainer"};
			Root.Add(_hierarchyContainer);

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			OnHierarchyChanged();
		}

		public void RefreshSelectedLabel()
		{
			SelectedLabel?.RefreshLabel();
		}

		public string SelectedElementLabel() => GetLabel(SelectedComponent).Replace(" ", "");

		protected override void OnDestroy()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;

			foreach (var bussElement in Components)
			{
				bussElement.StyleSheetsChanged -= OnBussStyleSheetChange;
			}
		}

		private void ChangeSelectedLabel(IndentedLabelVisualElement newLabel, bool setInHierarchy = true)
		{
			SelectedLabel?.Deselect();
			SelectedLabel = newLabel;
			SelectedLabel?.Select();

			if (!setInHierarchy) return;

			Selection.SetActiveObjectWithContext(SelectedLabel?.RelatedGameObject, SelectedLabel?.RelatedGameObject);
		}

		private string GetLabel(BussElement component)
		{
			if (!component) return String.Empty; // if the component has been destroyed, we cannot reason about it.

			string label = string.IsNullOrWhiteSpace(component.Id)
				? component.name
				: BussNameUtility.AsIdSelector(component.Id);

			foreach (string className in component.Classes)
			{
				label += " " + BussNameUtility.AsClassSelector(className);
			}

			return label;
		}

		private void OnBussStyleSheetChange()
		{
			if (!_hasDelayedChangeCallback)
			{
				_hasDelayedChangeCallback = true;
				EditorApplication.delayCall += () =>
				{
					RefreshStyleSheets();
					_hasDelayedChangeCallback = false;
					BussStyleSheetChange?.Invoke();
				};
			}
		}

		private void OnHierarchyChanged()
		{
			RefreshTree();
			HierarchyChanged?.Invoke();
			OnBussStyleSheetChange();
		}

		private void OnMouseClicked(IndentedLabelVisualElement newLabel)
		{
			// Deselecting label in case of clicking same one for the second time
			if (SelectedLabel != null)
			{
				if (SelectedLabel == newLabel)
				{
					SelectedLabel.Deselect();
					SelectedLabel = null;
					Selection.SetActiveObjectWithContext(null, null);
					SelectionCleared?.Invoke();
					return;
				}
			}

			ChangeSelectedLabel(newLabel);
		}

		private void OnObjectRegistered(BussElement registeredObject)
		{
			BussStyleSheet styleSheet = registeredObject.StyleSheet;

			if (styleSheet == null) return;

			if (!StyleSheets.Contains(styleSheet))
			{
				StyleSheets.Add(styleSheet);
			}

			registeredObject.StyleSheetsChanged -= OnBussStyleSheetChange;
			registeredObject.StyleSheetsChanged += OnBussStyleSheetChange;
		}

		private void OnSelectionChanged()
		{
			IndentedLabelVisualElement indentedLabelVisualElement =
				_spawnedLabels.Find(label => label.RelatedGameObject == Selection.activeGameObject);

			if (indentedLabelVisualElement != null)
			{
				if (indentedLabelVisualElement.RelatedGameObject != null)
				{
					SelectedComponent = indentedLabelVisualElement.RelatedGameObject.GetComponent<BussElement>();
					ChangeSelectedLabel(indentedLabelVisualElement, false);
				}
			}
			else
			{
				SelectedComponent = null;
				ChangeSelectedLabel(null, false);
			}

			SortStyleSheets();
		}

		private void RefreshStyleSheets()
		{
			StyleSheets.Clear();

			BussConfiguration.OptionalInstance.DoIfExists(config =>
			{
				foreach (BussStyleSheet styleSheet in config.DefaultBeamableStyleSheetSheets)
				{
					if (styleSheet != null)
						StyleSheets.Add(styleSheet);
				}

				foreach (BussStyleSheet styleSheet in config.GlobalStyleSheets)
				{
					if (styleSheet != null)
						StyleSheets.Add(styleSheet);
				}
			});

			foreach (BussElement component in Components)
			{
				var styleSheet = component.StyleSheet;

				if (styleSheet == null) continue;

				if (!StyleSheets.Contains(styleSheet))
				{
					StyleSheets.Add(styleSheet);
				}
			}
		}

		private void RefreshTree()
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
		}

		private void SortStyleSheets()
		{
			if (SelectedComponent == null)
			{
				return;
			}

			List<BussStyleSheet> selectedComponentAllStyleSheets = SelectedComponent.AllStyleSheets;

			if (selectedComponentAllStyleSheets.Count == 0)
			{
				return;
			}

			BussStyleSheet firstStyle = selectedComponentAllStyleSheets[selectedComponentAllStyleSheets.Count - 1];

			StyleSheets.Remove(firstStyle);
			StyleSheets.Insert(0, firstStyle);
		}

		private void Traverse(GameObject gameObject, int currentLevel)
		{
			if (!gameObject) return; // if the gameobject has been destroyed, we cannot traverse it.

			BussElement foundComponent = gameObject.GetComponent<BussElement>();

			if (foundComponent != null)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement();
				label.Setup(foundComponent.gameObject, bussElement => GetLabel(foundComponent), OnMouseClicked,
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
