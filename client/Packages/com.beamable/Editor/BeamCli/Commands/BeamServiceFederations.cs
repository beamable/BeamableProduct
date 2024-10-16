
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceFederations
	{
		public string beamoName;
		public string routingKey;
		public Beamable.Server.FederationsConfig federations;
	}
}
