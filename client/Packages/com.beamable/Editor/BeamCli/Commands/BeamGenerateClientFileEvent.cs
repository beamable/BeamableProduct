
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamGenerateClientFileEvent
	{
		public string beamoId;
		public string filePath;
	}
}
