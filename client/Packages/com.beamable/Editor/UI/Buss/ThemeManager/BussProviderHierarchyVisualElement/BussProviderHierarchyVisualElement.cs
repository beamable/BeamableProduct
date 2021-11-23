using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using UnityEngine.UI;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussProviderHierarchyVisualElement : ComponentBasedHierarchyVisualElement<BussElement>	
	{
		public new class UxmlFactory : UxmlFactory<BussProviderHierarchyVisualElement, UxmlTraits>
		{
		}

		public BussProviderHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussProviderHierarchyVisualElement)}/{nameof(BussProviderHierarchyVisualElement)}") { }

		protected override string GetLabel(BussElement component)
		{
			return string.IsNullOrEmpty(component.Id) || string.IsNullOrWhiteSpace(component.Id)
				? component.name
				: component.Id;
		}
	}
}
