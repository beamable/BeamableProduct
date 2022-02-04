using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
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
		public event Action BussStyleSheetChange;

		public List<BussStyleSheet> StyleSheets
		{
			get;
		} = new List<BussStyleSheet>();

		public void ForceRebuild()
		{
			StyleSheets.Clear();
			RefreshTree();
		}

		protected override string GetLabel(BussElement component)
		{
			string label = string.IsNullOrWhiteSpace(component.Id) ? component.name : BussNameUtility.AsIdSelector(component.Id);

			foreach (string className in component.Classes)
			{
				label += " " + BussNameUtility.AsClassSelector(className);
			}

			return label;
		}

		protected override void OnHierarchyChanged()
		{
			StyleSheets.Clear();
			base.OnHierarchyChanged();
		}

		protected override void OnSelectionChanged()
		{
			base.OnSelectionChanged();
			SortStyleSheets();
		}

		private void OnBussStyleSheetChange()
		{
			BussStyleSheetChange?.Invoke();
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
	}
}
