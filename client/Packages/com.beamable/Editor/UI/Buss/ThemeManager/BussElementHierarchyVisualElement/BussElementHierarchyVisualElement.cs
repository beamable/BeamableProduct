using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
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
	public class BussElementHierarchyVisualElement : ComponentBasedHierarchyVisualElement<BussElement>
	{
		private bool _hasDelayedChangeCallback;

		public event Action BussStyleSheetChange;

		public List<BussStyleSheet> StyleSheets
		{
			get;
		} = new List<BussStyleSheet>();

		public void ForceRebuild(GameObject selectedGameObject = null)
		{
			StyleSheets.Clear();
			RefreshTree();
			OnBussStyleSheetChange();

			if (selectedGameObject != null)
			{
				Selection.activeGameObject = selectedGameObject;
				OnSelectionChanged();
			}
		}

		protected override string GetLabel(BussElement component)
		{
			if (!component) return String.Empty; // if the component has been destroyed, we cannot reason about it.

			string label = string.IsNullOrWhiteSpace(component.Id) ? component.name : BussNameUtility.AsIdSelector(component.Id);

			foreach (string className in component.Classes)
			{
				label += " " + BussNameUtility.AsClassSelector(className);
			}

			return label;
		}

		protected override void OnHierarchyChanged()
		{
			base.OnHierarchyChanged();
			OnBussStyleSheetChange();
		}

		protected override void OnSelectionChanged()
		{
			base.OnSelectionChanged();
			SortStyleSheets();
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

		protected override void OnObjectRegistered(BussElement registeredObject)
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

		protected override void OnDestroy()
		{
			base.OnDestroy();
			foreach (var bussElement in Components)
			{
				bussElement.StyleSheetsChanged -= OnBussStyleSheetChange;
			}
		}

		public void RefreshSelectedLabel()
		{
			SelectedLabel.RefreshLabel();
		}
	}
}
