
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamManifestChecksum
	{
		public string id;
		public string checksum;
		public long createdAt;
	}
}
