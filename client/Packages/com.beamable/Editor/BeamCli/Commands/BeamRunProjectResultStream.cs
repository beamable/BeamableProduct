
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamRunProjectResultStream
	{
		public string serviceId;
		public string message;
		public float progressRatio;
	}
}
