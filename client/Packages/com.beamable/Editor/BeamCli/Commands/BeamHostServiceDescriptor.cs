
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamHostServiceDescriptor
	{
		public string service;
		public int processId;
		public int healthPort;
		public string routingKey;
		public long startedByAccountId;
		public string[] groups;
		public Beamable.Common.FederationInstance[] federations;
	}
}
