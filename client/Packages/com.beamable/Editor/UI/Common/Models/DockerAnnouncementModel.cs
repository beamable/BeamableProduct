using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI.Buss;
using System;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Models
{
	public class DockerAnnouncementModel : AnnouncementModelBase
	{
		public string TitleLabelText => "DOCKER IS NOT INSTALLED";
		public string DescriptionLabelText => "You need to install Docker to use the Beamable C# Microservices Feature";
		public string InstallButtonText => "Install";

		public Action OnInstall;

		public override BeamableVisualElement CreateVisualElement()
		{
			return new DockerAnnouncementVisualElement() { DockerAnnouncementModel = this };
		}
	}
}
