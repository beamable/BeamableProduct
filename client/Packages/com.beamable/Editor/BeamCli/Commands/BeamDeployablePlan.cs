
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamDeployablePlan
	{
		public string builtFromRemoteChecksum;
		public BeamDeployMode mode;
		public Beamable.Api.Autogenerated.Models.ManifestView manifest;
		public BeamDeploymentDiffSummary diff;
		public System.Collections.Generic.List<string> servicesToUpload;
		public bool ranHealthChecks;
		public int changeCount;
	}
}