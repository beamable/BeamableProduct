
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamListCommandResult
	{
		public System.Collections.Generic.List<Beamable.Common.BeamCli.Contracts.ServiceInfo> localServices;
		public System.Collections.Generic.List<Beamable.Common.BeamCli.Contracts.ServiceInfo> localStorages;
	}
}
