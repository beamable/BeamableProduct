
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamTailLogMessage
	{
		public string timeStamp;
		public string message;
		public string logLevel;
		public string raw;
	}
}
