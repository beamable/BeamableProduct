
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamStartDockerCommandOutput
	{
		public bool attempted;
		public bool alreadyRunning;
		public bool unavailable;
		public string dockerDesktopUrl;
		public string downloadUrl;
	}
}
