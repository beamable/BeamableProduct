
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamDockerServiceDescriptor
	{
		public string service;
		public string serviceType;
		public string containerId;
		public int healthPort;
		public int dataPort;
		public string routingKey;
		public long startedByAccountId;
		public string[] groups;
		public Beamable.Common.FederationInstance[] federations;
	}
}
