
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamRunningService
	{
		public string name;
		public string routingKey;
		public string fullName;
		public int instanceCount;
		public bool trafficFilterEnabled;
		public long authorPlayerId;
		public System.Collections.Generic.List<BeamRunningFederation> federations;
	}
}
