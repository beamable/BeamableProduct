using Beamable.Editor.UI.Buss;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	// TODO: change generic type from Text to BussProvider before final merge
	public class BussProviderHierarchyVisualElement : ComponentBasedHierarchyVisualElement<Text>	
	{
		public new class UxmlFactory : UxmlFactory<BussProviderHierarchyVisualElement, UxmlTraits>
		{
		}

		public BussProviderHierarchyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussProviderHierarchyVisualElement)}/{nameof(BussProviderHierarchyVisualElement)}") { }

		protected override string GetLabel(Text component)
		{
			return component.name;
		}
	}
}
