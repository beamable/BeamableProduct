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
		public string TitleLabelText => "Beamable Upgrade Available!";
		public string InstallButtonText => "Upgrade";
		public string WhatsNewButtonText => "What's New";
		public string DescriptionLabelText { get; private set; }

		public Action OnInstall;
		public Action OnIgnore;
		public Action OnWhatsNew;

		private string _blurbBlogText = "Read all about it on the Beamable Blog.";

		public void SetDescription(string version, bool addBlurbBlogText = false)
		{
			var blurbText = addBlurbBlogText ? _blurbBlogText : string.Empty;
			DescriptionLabelText = $"Good news, {version} has been released! You can upgrade now and check out the new features. {blurbText}";
		}

		public override BeamableVisualElement CreateVisualElement()
		{
			return new UpdateAvailableAnnouncementVisualElement()
			{
				UpdateAvailableAnnouncementModel = this
			};
		}
	}
}
