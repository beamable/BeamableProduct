using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
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
		public BussElementHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/ComponentBasedHierarchyVisualElement") { }

		protected override string GetLabel(BussElement component)
		{
			return string.IsNullOrWhiteSpace(component.Id) ? component.name : component.Id;
		}
	}
}
