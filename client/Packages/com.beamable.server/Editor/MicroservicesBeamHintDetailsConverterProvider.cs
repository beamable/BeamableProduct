using Beamable.Common.Assistant;
using Beamable.Editor.Assistant;
using Beamable.Editor.Reflection;
using Beamable.Server.Editor.DockerCommands;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class MicroservicesBeamHintDetailsConverterProvider : BeamHintDetailConverterProvider
	{
		/// <summary>
		/// Converter that handles the <see cref="BeamHintIds.ID_DOCKER_PROCESS_NOT_RUNNING"/> hint.
		/// </summary>
		[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
								 BeamHintType.Validation, "", "DockerProcessNotRunning",
								 "HintDetailsSingleTextButton")]
		public static void DockerNotRunningConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
		{
			var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;
			injectionBag.SetLabel(validationIntro, "hintText");
			injectionBag.SetButtonLabel("Try to Open Docker Desktop", "hintButton");
			injectionBag.SetButtonClicked(() =>
			{
				_ = DockerCommand.RunDockerProcess();
			}, "hintButton");
		}

		/// <summary>
		/// Converter that handles the <see cref="BeamHintIds.ID_DOCKER_PROCESS_NOT_RUNNING"/> hint.
		/// </summary>
		[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
								 BeamHintType.Validation, "", "InstallDockerProcess",
								 "HintDetailsSingleTextButton")]
		public static void InstallDockerProcessConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
		{
			var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;
			injectionBag.SetLabel(validationIntro, "hintText");
			injectionBag.SetButtonLabel("Go to Docker and Docker Desktop's Installation Guide", "hintButton");
			injectionBag.SetButtonClicked(() =>
			{
				Application.OpenURL("https://docs.docker.com/get-docker/");
			}, "hintButton");
		}
	}
}
