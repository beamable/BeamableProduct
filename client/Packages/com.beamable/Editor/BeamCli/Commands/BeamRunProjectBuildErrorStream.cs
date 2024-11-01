
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamRunProjectBuildErrorStream
	{
		public string serviceId;
		public BeamProjectErrorReport report;
	}
}
