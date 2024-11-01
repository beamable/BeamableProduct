
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamLocalContentManifest
	{
		public string ManifestId;
		public BeamLocalContentManifestEntry Entries;
	}
}
