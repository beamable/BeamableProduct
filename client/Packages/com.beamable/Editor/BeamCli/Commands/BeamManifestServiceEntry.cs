
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamManifestServiceEntry
	{
		public string beamoId;
		public bool shouldBeEnabledOnRemote;
		public string csprojPath;
		public System.Collections.Generic.List<string> storageDependencies;
		public System.Collections.Generic.List<BeamUnityAssemblyReferenceData> unityReferences;
	}
}
