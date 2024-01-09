
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceDeployLogResult
	{
		public string Message;
		public Beamable.Common.BeamCli.LogLevel Level;
		public string TimeStamp;
	}
}
