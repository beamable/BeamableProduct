using Beamable.Api;
using Beamable.Editor;

namespace Beamable.Server.Editor.ManagerClient
{
	public static class BeamableExtensions
	{
		private static MicroserviceManager _manager;

		public static MicroserviceManager GetMicroserviceManager(this BeamEditorContext de)
		{
			return de.ServiceScope.GetService<MicroserviceManager>();
		}
	}
}
