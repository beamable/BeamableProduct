
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamServerKillCommandResult
	{
		public System.Collections.Generic.List<BeamServerDescriptor> stoppedServers;
	}
}