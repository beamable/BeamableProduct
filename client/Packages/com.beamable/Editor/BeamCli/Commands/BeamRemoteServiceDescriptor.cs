
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamRemoteServiceDescriptor
	{
		public string service;
		public string routingKey;
		public long startedByAccountId;
		public string[] groups;
		public Beamable.Common.FederationInstance[] federations;
	}
}
