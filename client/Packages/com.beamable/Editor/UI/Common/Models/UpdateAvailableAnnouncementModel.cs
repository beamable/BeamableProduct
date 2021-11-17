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
	public class UpdateAvailableAnnouncementModel : AnnouncementModelBase
	{
		public string TitleLabelText => "NEW VERSION THAT IS AVAILABLE";
		public string InstallButtonText => "Install";
		public string WhatsNewButtonText => "What's New";

		public string DescriptionLabelText
		{
			get;
			private set;
		} =
			"Beamable 0.0.0 has been released! You should upgrade and check out the new features";

		public Action OnInstall;
		public Action OnIgnore;
		public Action OnWhatsNew;

		public void SetPackageVersion(string version)
		{
			DescriptionLabelText =
				$"Beamable {version} has been released! You should upgrade and check out the new features";
		}

		public override BeamableVisualElement CreateVisualElement()
		{
			return new UpdateAvailableAnnouncementVisualElement() { UpdateAvailableAnnouncementModel = this };
		}
	}
}
