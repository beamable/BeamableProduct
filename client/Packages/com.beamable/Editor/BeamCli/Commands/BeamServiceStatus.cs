
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceStatus
	{
		public string service;
		public string serviceType;
		public System.Collections.Generic.List<BeamServicesForRouteCollection> availableRoutes;
	}
}