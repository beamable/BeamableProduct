
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceStorageReference
	{
		public string id;
		public string storageType;
		public bool enabled;
		public string templateId;
		public string checksum;
	}
}
