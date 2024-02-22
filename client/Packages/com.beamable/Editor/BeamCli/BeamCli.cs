using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using UnityEngine;

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
				
#if BEAMABLE_DEVELOPER
				// if we are developers, then we should always use the latest beam version
				return true;
#else
				// but if we are not developers, then the global version must match the SDK version.
				var buffer = comm.GetMessageBuffer();
				if (!PackageVersion.TryFromSemanticVersionString(buffer, out var version))
				{
					return false;
				}
				
				if (BeamableEnvironment.SdkVersion != version)
				{
					return false;
				}
				
				Debug.Log("Using CLI");
				return true;
#endif
			}
			catch
			{
				return false;
			}
		}
	}
}
