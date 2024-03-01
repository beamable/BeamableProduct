
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamProjectErrorReport
	{
		public bool isSuccess;
		public System.Collections.Generic.List<BeamProjectErrorResult> errors;
	}
}
