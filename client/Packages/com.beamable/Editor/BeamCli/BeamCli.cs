using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;

namespace Beamable.Editor.BeamCli
{
	public class BeamCli
	{
		private readonly IDependencyProvider _provider;

		public BeamCli(IDependencyProvider provider)
		{
			_provider = provider;
		}

		public BeamCommands Command => DependencyBuilder.Instantiate<BeamCommands>(_provider);

		public async Promise<bool> IsAvailable()
		{
			var comm = new BeamCommand(_provider.GetService<BeamableDispatcher>());
			comm.SetCommand("beam --version");
			comm.AutoLogErrors = false;
			try
			{
				await comm.Run();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
