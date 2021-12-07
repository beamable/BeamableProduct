using Beamable.Editor.UI.Buss;

namespace Beamable.Editor.BeamableAssistant.Components
{
	public class BeamableAssistantComponent : BeamableVisualElement
	{
		public BeamableAssistantComponent(string name) : base($"{BeamableAssistantConstants.COMP_PATH}/{name}/{name}")
		{

		}
	}
}
