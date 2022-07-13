using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Beamable.Common;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Models
{
	public interface IWebsiteHook
	{
		void OpenUrl(string url);
	}

	public class WebsiteHook : IWebsiteHook
	{
		public void OpenUrl(string url)
		{
			Application.OpenURL(url);
		}
	}
}
