using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Beamable.Common;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.Toolbox.Models;

namespace Beamable.Editor.Tests
{
	public class MockWebsiteHook : IWebsiteHook
	{
		public string Url { get; set; }
		public void OpenUrl(string url)
		{
			Url = url;
		}
	}
}

