using Beamable.Common;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;

namespace Tests
{
	public class DummyCli : IBeamCli
	{
		public Promise OnReady { get; }
		public BeamCommands Command { get; } = null;
		public Promise<bool> IsAvailable()
		{
			var promise = new Promise<bool>();
			promise.CompleteSuccess(false);
			return promise;
		}

		public Promise Init()
		{
			return Promise.Success;
		}

		public DummyCli()
		{
			OnReady = Init();
		}
	}
}
