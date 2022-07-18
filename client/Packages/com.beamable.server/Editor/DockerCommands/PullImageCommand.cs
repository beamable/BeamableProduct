using Beamable.Common;
using Beamable.Server.Editor.CodeGen;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
	public class PullImageCommand : DockerCommandReturnable<bool>
	{
		private readonly string _imageAndTag;

		public static PullImageCommand PullBeamService() =>
			new PullImageCommand($"{DockerfileGenerator.BASE_IMAGE}:{DockerfileGenerator.BASE_TAG}");

		public PullImageCommand(string imageAndTag)
		{
			_imageAndTag = imageAndTag;
			WriteCommandToUnity = false;
			WriteLogToUnity = false;
		}

		public override string GetCommandString()
		{
			var platform = MicroserviceConfiguration.Instance.DockerCPUArchitecture;

			var platformStr = "";
#if !BEAMABLE_DISABLE_AMD_MICROSERVICE_BUILDS
			platformStr = $"--platform {platform}";
#endif

			return $"{DockerCmd} pull {platformStr} {_imageAndTag}";
		}

		protected override void Resolve()
		{
			Promise.CompleteSuccess(_exitCode == 0);
		}
	}
}
