
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamListDeploymentsCommandOutput
	{
		public Beamable.Api.Autogenerated.Models.ManifestView[] deployments;
	}
}
