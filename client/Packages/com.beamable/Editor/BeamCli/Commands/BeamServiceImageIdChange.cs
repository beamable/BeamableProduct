
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceImageIdChange
	{
		public string service;
		public string oldImageId;
		public string nextImageId;
	}
}
