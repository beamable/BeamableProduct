using Beamable.UI.Buss;
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
		protected override string GetLabel(BussElement component)
		{
			return string.IsNullOrWhiteSpace(component.Id) ? component.name : component.Id;
		}

		protected override void OnSelectionChanged()
		{
			IndentedLabelVisualElement indentedLabelVisualElement = SpawnedLabels.Find(label => label.RelatedGameObject == Selection.activeGameObject);
			
			if (indentedLabelVisualElement != null)
			{
				ChangeSelectedLabel(indentedLabelVisualElement, false);
			}
		}
	}
}
