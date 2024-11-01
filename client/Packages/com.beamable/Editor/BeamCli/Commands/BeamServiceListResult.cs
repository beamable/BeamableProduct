
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamServiceListResult
	{
		public System.Collections.Generic.List<bool> ExistInLocal;
		public System.Collections.Generic.List<bool> ExistInRemote;
		public System.Collections.Generic.List<bool> IsRunningRemotely;
		public bool IsDockerRunning;
		public System.Collections.Generic.List<string> BeamoIds;
		public System.Collections.Generic.List<bool> ShouldBeEnabledOnRemote;
		public System.Collections.Generic.List<bool> IsRunningLocally;
		public System.Collections.Generic.List<string> ProtocolTypes;
		public System.Collections.Generic.List<string> ImageIds;
		public System.Collections.Generic.List<string> ContainerNames;
		public System.Collections.Generic.List<string> ContainerIds;
		public System.Collections.Generic.List<string> LocalHostPorts;
		public System.Collections.Generic.List<string> LocalContainerPorts;
		public System.Collections.Generic.List<string> Dependencies;
		public System.Collections.Generic.List<string> ProjectPath;
		public System.Collections.Generic.List<string> UnityAssemblyDefinitions;
	}
}
