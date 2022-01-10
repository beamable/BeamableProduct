using Beamable.Editor.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class VariableConnectionVisualElement : BeamableVisualElement
	{
		public VariableConnectionVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStylePropertyVisualElement)}/" +
			$"{nameof(VariableConnectionVisualElement)}/{nameof(VariableConnectionVisualElement)}") { }

		private VisualElement _mainElement;

		public override void Refresh()
		{
			base.Refresh();
			_mainElement = Root.Q("variableConnectionElement");
		}

		public void Setup(bool hasVariable)	// temporary parameter
		{
			Refresh();
		}
	}
}
