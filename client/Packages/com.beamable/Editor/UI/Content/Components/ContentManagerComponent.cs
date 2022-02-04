using Beamable.Editor.UI.Components;

namespace Beamable.Editor.Content.Components
{
	public class ContentManagerComponent : BeamableVisualElement
	{
		public ContentManagerComponent(string name) : base($"{ContentManagerConstants.COMP_PATH}/{name}/{name}")
		{

		}
	}
}
