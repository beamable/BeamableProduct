
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServicesForRouteCollection
	{
		public string routingKey;
		public System.Collections.Generic.List<BeamServiceInstance> instances;
	}
}
