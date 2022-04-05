using Beamable.Common;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
	public class CheckImageReturnableCommand : DockerCommandReturnable<bool>
	{
		public string ContainerName { get; }
		public bool IsRunning { get; protected set; }

		public CheckImageReturnableCommand(IDescriptor descriptor)
		   : this(descriptor.ContainerName)
		{

		}

		public CheckImageReturnableCommand(string containerName)
		{
			ContainerName = containerName;
		}

		public override string GetCommandString()
		{
			var command = $"{DockerCmd} ps -f \"name={ContainerName}\"";
			return command;
		}

		protected override void HandleStandardOut(string data)
		{
			base.HandleStandardOut(data);

			// 7c7e95c20caf        tunafish            "dotnet tunafish.dll"   7 hours ago         Up 7 hours          0.0.0.0:56798->80/tcp   tunafishcontainer

			// TODO: We could use a better text matching system, but for now...
			// TODO: Support other languages
			if (data != null && data.Contains($" {ContainerName}") && data.Contains(" Up "))
			{
				IsRunning = true;
			}
		}
		
		protected override void Resolve()
		{
			Promise.CompleteSuccess(IsRunning);
		}
	}
	
	public class CheckImageCodeGenErrorCommand : DockerCommandReturnable<bool>
	{
		public string ContainerName { get; }
		public bool HasCodeGenError { get; private set; }

		public CheckImageCodeGenErrorCommand(IDescriptor descriptor)
			: this(descriptor.ContainerName)
		{

		}

		public CheckImageCodeGenErrorCommand(string containerName)
		{
			ContainerName = containerName;
		}

		public override string GetCommandString()
		{
			var command = $"{DockerCmd} logs --tail=50 {ContainerName}";
			return command;
		}

		protected override void HandleStandardOut(string data)
		{
			base.HandleStandardOut(data);
			
			// dependency or code gen errors
			if (data != null && (data.Contains("CS0012") || data.Contains("CS0246")))
			{
				HasCodeGenError = true;
			}
		}
		protected override void Resolve()
		{
			Promise.CompleteSuccess(HasCodeGenError);
		}
	}

	public class CheckImageCommand : CheckImageReturnableCommand
	{
		public CheckImageCommand(MicroserviceDescriptor descriptor) : base(descriptor)
		{
		}

		public new Promise<bool> Start()
		{
			return Start(null);
		}
	}
}
