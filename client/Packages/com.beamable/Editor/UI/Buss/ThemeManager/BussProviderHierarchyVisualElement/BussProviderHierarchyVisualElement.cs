using Beamable.Editor.UI.Buss;
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
	// TODO: change generic type from Text to BussElement before final merge
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
