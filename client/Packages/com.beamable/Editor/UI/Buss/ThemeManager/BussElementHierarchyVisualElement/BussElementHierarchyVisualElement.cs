using Beamable.UI.Buss;
using System.Collections.Generic;
using UnityEditor;
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
		public List<BussStyleSheet> StyleSheets
		{
			get;
		} = new List<BussStyleSheet>();

		protected override string GetLabel(BussElement component)
		{
			return string.IsNullOrWhiteSpace(component.Id) ? component.name : component.Id;
		}

		protected override void OnSelectionChanged()
		{
			IndentedLabelVisualElement indentedLabelVisualElement =
				SpawnedLabels.Find(label => label.RelatedGameObject == Selection.activeGameObject);

			if (indentedLabelVisualElement != null)
			{
				ChangeSelectedLabel(indentedLabelVisualElement, false);
			}
		}

		protected override void OnObjectRegistered(BussElement registeredObject)
		{
			BussStyleSheet styleSheet = registeredObject.StyleSheet;
			
			if(styleSheet == null) return;

			if (!StyleSheets.Contains(styleSheet))
			{
				StyleSheets.Add(styleSheet);
			}
		}

		protected override void OnHierarchyChanged()
		{
			StyleSheets.Clear();
			base.OnHierarchyChanged();
		}
	}
}
