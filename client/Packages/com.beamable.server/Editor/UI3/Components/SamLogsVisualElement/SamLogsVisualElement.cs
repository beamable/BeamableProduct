using Beamable.Common;
using Beamable.Editor.UI.Components;

namespace Beamable.Editor.Microservice.UI3.Components
{
	public class SamLogsVisualElement : BeamableVisualElement
	{
		public SamLogsVisualElement() : base(
			$"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/UI3/Components/{nameof(SamLogsVisualElement)}/{nameof(SamLogsVisualElement)}")
		{
			
		}
	}
}
