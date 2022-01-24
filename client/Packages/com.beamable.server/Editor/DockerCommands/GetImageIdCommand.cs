using Beamable.Platform.SDK;
using Beamable.Server.Editor.DockerCommands;
using System;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
	public class GetImageIdCommand : DockerCommandReturnable<string>
	{
		public string ImageName { get; }

		public GetImageIdCommand(IDescriptor descriptor)
		{
			ImageName = descriptor.ImageName;
		}
		public override string GetCommandString()
		{
			return $"{DockerCmd} images -q {ImageName}";
		}

		protected override void Resolve()
		{
			if (StandardOutBuffer?.Length > 0)
			{
				Promise.CompleteSuccess(StandardOutBuffer.Trim());
				return;
			}
			// there is no built image, we shouldn't log an error, we should just know that empty string means "not built".
			Promise.CompleteSuccess(string.Empty);
		}
	}
}
