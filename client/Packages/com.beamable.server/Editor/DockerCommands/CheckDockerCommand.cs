using Beamable.Common;
using UnityEditor;

namespace Beamable.Server.Editor.DockerCommands
{
	public class CheckDockerCommand : DockerCommandReturnable<bool>
	{
		static bool DockerCheckPerformed
		{
			get => SessionState.GetBool(nameof(DockerCheckPerformed), false);
			set => SessionState.SetBool(nameof(DockerCheckPerformed), value);
		}

		public override bool DockerRequired => false;

		public static async Promise<bool> PerformCheck()
		{
			var result = !DockerNotInstalled;
			if (!DockerCheckPerformed)
			{
				result = await new CheckDockerCommand().StartAsync();
			}

			return result;
		}

		public override string GetCommandString()
		{
			ClearDockerInstallFlag();
			var command = $"{DockerCmd} --version";
			return command;
		}

		protected override void Resolve()
		{
			var isInstalled = _exitCode == 0;
			DockerNotInstalled = !isInstalled;
			DockerCheckPerformed = true;
			Promise.CompleteSuccess(isInstalled);
		}
	}
}
