
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamAccountMeExternalIdentity
	{
		public string providerNamespace;
		public string providerService;
		public string userId;
	}
}
