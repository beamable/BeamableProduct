using Beamable.Editor.UI.Components;

namespace Beamable.Editor.Toolbox.UI.Components
{
	public class ToolboxComponent : BeamableVisualElement
	{
		public ToolboxComponent(string name) : base($"{ToolboxConstants.COMP_PATH}/{name}/{name}")
		{

		}
	}
}
