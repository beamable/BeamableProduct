
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamCliServiceManifest
	{
		public string id;
		public long created;
		public System.Collections.Generic.List<BeamCliServiceReference> manifest;
		public System.Collections.Generic.List<BeamCliServiceStorageReference> storageReference;
		public long createdByAccountId;
		public string comments;
	}
}
