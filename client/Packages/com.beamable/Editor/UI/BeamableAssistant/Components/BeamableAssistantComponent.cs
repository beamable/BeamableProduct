using Beamable.Editor.UI.Buss;

namespace Beamable.Editor.Assistant
{
	public class BeamableAssistantComponent : BeamableVisualElement
	{
		public BeamableAssistantComponent(string name) : base($"{BeamableAssistantConstants.COMP_PATH}/{name}/{name}") { }
	}
}
