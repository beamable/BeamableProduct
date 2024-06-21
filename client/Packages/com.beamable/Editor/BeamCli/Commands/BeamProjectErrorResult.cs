
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamProjectErrorResult
	{
		public string level;
		public string formattedMessage;
		public string uri;
		public int line;
		public int column;
	}
}
