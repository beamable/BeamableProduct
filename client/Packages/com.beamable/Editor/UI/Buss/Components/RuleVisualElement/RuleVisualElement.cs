using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Toolbox.Components
{
	public class RuleVisualElement : BeamableVisualElement
	{
		public RuleVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_COMPONENTS_PATH}/{nameof(RuleVisualElement)}/{nameof(RuleVisualElement)}")
		{
		}
		
		public new class UxmlFactory : UxmlFactory<RuleVisualElement, UxmlTraits>
		{
		}

		public override void Refresh()
		{
			base.Refresh();
			
			var lel = Root.Q<DropdownVisualElement>("propertyDropdown");
			lel.Setup(BussStyle.Keys.ToList(), i => Debug.LogWarning(""));
			lel.Refresh();
		}
	}
}
