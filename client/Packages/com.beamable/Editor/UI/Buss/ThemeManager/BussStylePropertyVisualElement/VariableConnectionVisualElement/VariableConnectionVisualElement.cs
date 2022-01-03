using Beamable.Editor.UI.Buss;
using UnityEngine.UIElements;

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
