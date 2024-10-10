
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamPlanReleaseProgress
	{
		public string name;
		public float ratio;
		public bool isKnownLength;
		public string serviceName;
	}
}
