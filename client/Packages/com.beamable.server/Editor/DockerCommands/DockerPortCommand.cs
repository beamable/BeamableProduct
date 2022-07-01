namespace Beamable.Server.Editor.DockerCommands
{
	public class DockerPortResult
	{
		public bool ContainerExists;
		public string LocalAddress;
		public string LocalPort;
	}

	public class DockerPortCommand : DockerCommandReturnable<DockerPortResult>
	{
		private readonly IDescriptor _descriptor;
		private readonly int _containerPort;

		public DockerPortCommand(IDescriptor descriptor, int containerPort)
		{
			_descriptor = descriptor;
			_containerPort = containerPort;
		}

		public override string GetCommandString()
		{
			return $"{DockerCmd} port {_descriptor.ContainerName} {_containerPort}";
		}

		protected override void Resolve()
		{
			if (StandardErrorBuffer != null && StandardErrorBuffer.Contains("Error"))
			{
				Promise.CompleteSuccess(new DockerPortResult
				{
					ContainerExists = false
				});
			}
			else
			{
				UnityEngine.Debug.Log("OUT:" + StandardOutBuffer);
				var addr = StandardOutBuffer.Trim();
				Promise.CompleteSuccess(new DockerPortResult
				{
					ContainerExists = true,
					LocalAddress = addr,
					LocalPort = addr.Split(':')[1]
				});
			}

		}
	}
}
