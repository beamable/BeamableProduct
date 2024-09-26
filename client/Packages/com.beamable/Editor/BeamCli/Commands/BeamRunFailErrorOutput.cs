
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamRunFailErrorOutput
	{
		public System.Collections.Generic.List<string> failedServices;
		public string message;
		public string invocation;
		public int exitCode;
		public string typeName;
		public string fullTypeName;
		public string stackTrace;
	}
}
