
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamLocalContentManifestEntry
	{
		public string FullId;
		public string TypeName;
		public string Name;
		public int CurrentStatus;
		public string Hash;
		public string[] Tags;
	}
}
