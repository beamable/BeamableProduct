using Beamable.Platform.SDK;
using Beamable.Server.Editor.DockerCommands;
using System;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
	public class GetImageIdCommand : DockerCommandReturnable<string>
	{
		public string ImageName { get; }
		private bool WasBuildLocally { get; }

		public GetImageIdCommand(IDescriptor descriptor)
		{
			ImageName = descriptor.ImageName;
			WasBuildLocally = BuildImageCommand.WasEverBuildLocally(descriptor;
		}
		public override string GetCommandString()
		{
			return $"{DockerCmd} images -q {ImageName}";
		}

		protected override void Resolve()
		{
			bool hasResult = WasBuildLocally && StandardOutBuffer?.Length > 0;
			// there is no built image, we shouldn't log an error, we should just know that empty string means "not built".
			Promise.CompleteSuccess(hasResult ? StandardOutBuffer.Trim() : string.Empty);
		}
	}
}
