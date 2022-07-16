using Beamable.Common;
using Beamable.Server.Editor.CodeGen;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
	public class PullImageCommand : DockerCommandReturnable<bool>
	{
		public static PullImageCommand PullBeamService() =>
			new PullImageCommand(DockerfileGenerator.BASE_IMAGE, DockerfileGenerator.BASE_TAG);

		private readonly string _image;
		private readonly string _tag;

		public PullImageCommand(string image, string tag)
		{
			_image = image;
			_tag = tag;
			WriteCommandToUnity = false;
			WriteLogToUnity = false;
		}

		public override string GetCommandString()
		{
			var image = $"{_image}:{_tag}";
			var platform = MicroserviceConfiguration.Instance.DockerCPUArchitecture;

			var platformStr = "";
			#if !BEAMABLE_DISABLE_AMD_MICROSERVICE_BUILDS
			platformStr = $"--platform {platform}";
			#endif

			return $"{DockerCmd} pull {platformStr} {image}";
		}

		protected override void Resolve()
		{
			Promise.CompleteSuccess(_exitCode == 0);
		}
	}
}
