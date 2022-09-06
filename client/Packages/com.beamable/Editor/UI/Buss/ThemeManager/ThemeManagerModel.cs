using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	public class ThemeManagerModel
	{
		public event Action Change;

		public readonly Dictionary<BussElement, int> FoundElements = new Dictionary<BussElement, int>();

		public List<BussStyleSheet> StyleSheets { get; } = new List<BussStyleSheet>();

		public IEnumerable<BussStyleSheet> WritableStyleSheets
		{
			get
			{
#if BEAMABLE_DEVELOPER
				return StyleSheets ?? Enumerable.Empty<BussStyleSheet>();
#else
				return StyleSheets?.Where(s => !s.IsReadOnly) ?? Enumerable.Empty<BussStyleSheet>();
#endif
			}
		}

		public BussElement SelectedElement { get; private set; }

		public string SelectedElementId =>
			SelectedElement != null ? BussNameUtility.AsIdSelector(SelectedElement.Id) : String.Empty;

		public BussStyleSheet SelectedElementStyleSheet => SelectedElement != null ? SelectedElement.StyleSheet : null;

		public ThemeManagerModel()
		{
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Selection.selectionChanged += OnSelectionChanged;

			OnHierarchyChanged();
		}

		public void Clear()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			Selection.selectionChanged -= OnSelectionChanged;
		}

		public void NavigationElementClicked(BussElement element)
		{
			Selection.activeGameObject = element.gameObject;
		}

		public void OnFocus()
		{
			Change?.Invoke();
		}

		public void OnIdChanged(string value)
		{
			SelectedElement.Id = BussNameUtility.CleanString(value);
			Change?.Invoke();
		}

		public void OnStylesheetChanged(UnityEngine.Object styleSheet)
		{
			BussStyleSheet newStyleSheet = (BussStyleSheet)styleSheet;

			if (SelectedElement != null)
			{
				SelectedElement.StyleSheet = newStyleSheet;
				Change?.Invoke();
			}
		}

		private void BussElementClicked(BussElement element)
		{
			SelectedElement = element;
			Change?.Invoke();
		}

		private void OnHierarchyChanged()
		{
			FoundElements.Clear();

			foreach (UnityEngine.Object foundObject in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject gameObject = (GameObject)foundObject;
				if (gameObject.transform.parent == null)
				{
					Traverse(gameObject, 0);
				}
			}

			Change?.Invoke();
		}

		private void OnObjectRegistered(BussElement registeredObject)
		{
			BussStyleSheet styleSheet = registeredObject.StyleSheet;

			if (styleSheet == null) return;

			if (!StyleSheets.Contains(styleSheet))
			{
				StyleSheets.Add(styleSheet);
			}

			// registeredObject.StyleSheetsChanged -= OnBussStyleSheetChange;
			// registeredObject.StyleSheetsChanged += OnBussStyleSheetChange;
		}

		private void OnSelectionChanged()
		{
			if (Selection.activeGameObject != null)
			{
				BussElement bussElement = Selection.activeGameObject.GetComponent<BussElement>();
				BussElementClicked(bussElement);
			}
			else
			{
				BussElementClicked(null);
			}
		}

		private void Traverse(GameObject gameObject, int currentLevel)
		{
			if (!gameObject) return; // if the gameobject has been destroyed, we cannot traverse it.

			BussElement foundComponent = gameObject.GetComponent<BussElement>();

			if (foundComponent != null)
			{
				FoundElements.Add(foundComponent, currentLevel);
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
