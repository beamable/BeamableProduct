
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamServiceRunProgressResult
	{
		public string BeamoId;
		public double LocalDeployProgress;
	}
}
