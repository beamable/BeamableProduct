
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamErrorResult
	{
		public string stackTrace;
		public string typeName;
		public string fullTypeName;
		public string message;
		public int exitCode;
		public string invocation;
	}
}
