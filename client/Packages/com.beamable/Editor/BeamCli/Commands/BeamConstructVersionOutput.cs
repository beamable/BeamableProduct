
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public partial class BeamConstructVersionOutput
	{
		public string versionString;
		public string versionPrefix;
		public string versionSuffix;
		public bool exists;
	}
}
