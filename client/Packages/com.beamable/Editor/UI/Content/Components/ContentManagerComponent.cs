using Beamable.Editor.UI.Components;
using static Beamable.Common.Constants.BeamableConstants.Features.ContentManager;

namespace Beamable.Editor.Content.Components
{
	public class ContentManagerComponent : BeamableVisualElement
	{
		public ContentManagerComponent(string name) : base($"{COMPONENTS_PATH}/{name}/{name}")
		{

		}
	}
}
