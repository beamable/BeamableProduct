
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceListResult
	{
		public bool IsLocal;
		public bool IsDockerRunning;
		public System.Collections.Generic.List<string> BeamoIds;
		public System.Collections.Generic.List<bool> ShouldBeEnabledOnRemote;
		public System.Collections.Generic.List<bool> RunningState;
		public System.Collections.Generic.List<string> ProtocolTypes;
		public System.Collections.Generic.List<string> ImageIds;
		public System.Collections.Generic.List<string> ContainerNames;
		public System.Collections.Generic.List<string> ContainerIds;
		public System.Collections.Generic.List<string> LocalHostPorts;
		public System.Collections.Generic.List<string> LocalContainerPorts;
		public System.Collections.Generic.List<string> Dependencies;
		public System.Collections.Generic.List<string> ProjectPath;
	}
}
