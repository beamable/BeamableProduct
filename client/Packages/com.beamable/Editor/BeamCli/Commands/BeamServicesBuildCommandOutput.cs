
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamServicesBuildCommandOutput
	{
		public string id;
		public string message;
		public bool isFailure;
	}
}
