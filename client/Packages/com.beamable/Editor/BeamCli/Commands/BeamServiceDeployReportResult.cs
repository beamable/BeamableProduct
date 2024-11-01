
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamServiceDeployReportResult
	{
		public bool Success;
		public string FailureReason;
	}
}
