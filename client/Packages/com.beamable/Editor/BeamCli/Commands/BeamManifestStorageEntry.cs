
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamManifestStorageEntry
	{
		public string beamoId;
		public string csprojPath;
		public bool shouldBeEnabledOnRemote;
	}
}
