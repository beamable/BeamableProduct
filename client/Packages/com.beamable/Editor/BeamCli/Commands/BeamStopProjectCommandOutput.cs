
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamStopProjectCommandOutput
	{
		public string serviceName;
		public BeamServiceInstance instance;
	}
}
