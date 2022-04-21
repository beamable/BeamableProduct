using Beamable.Common;

namespace Beamable.Server.Editor.DockerCommands
{
	public class StopImageReturnableCommand : DockerCommandReturnable<bool>
	{
		public string ContainerName { get; set; }

		public StopImageReturnableCommand(IDescriptor descriptor) : this(
			descriptor.ContainerName)
		{

		}

		public StopImageReturnableCommand(string containerName)
		{
			ContainerName = containerName;
			UnityLogLabel = "STOP";
			WriteCommandToUnity = false;
			WriteLogToUnity = false;
		}

		public override string GetCommandString()
		{
			var command = $"{DockerCmd} stop {ContainerName}";
			return command;
		}

		protected override void Resolve()
		{
			Promise?.CompleteSuccess(true);
		}
	}
	public class StopImageCommand : StopImageReturnableCommand
	{
		public StopImageCommand(IDescriptor descriptor) : base(descriptor)
		{
		}
		public StopImageCommand(string containerName) : base(containerName)
		{
		}

		public new Promise<bool> Start()
		{
			return StartAsync();
		}
	}
}
