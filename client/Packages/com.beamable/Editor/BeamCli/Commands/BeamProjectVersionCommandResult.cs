
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamProjectVersionCommandResult
	{
		public string[] projectPaths;
		public string[] packageNames;
		public string[] packageVersions;
	}
}
