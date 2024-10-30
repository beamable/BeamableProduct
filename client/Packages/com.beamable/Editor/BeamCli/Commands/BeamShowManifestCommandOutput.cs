
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamShowManifestCommandOutput
	{
		public string localRoutingKey;
		public System.Collections.Generic.List<BeamManifestServiceEntry> services;
		public System.Collections.Generic.List<BeamManifestStorageEntry> storages;
		public System.Collections.Generic.List<string> existingFederationIds;
		public System.Collections.Generic.List<string> availableFederationTypes;
	}
}
