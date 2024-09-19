
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamGenerateOApiCommandOutput
	{
		public string service;
		public bool isBuilt;
		public string openApi;
	}
}
