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
		public void OpenUrl(string url)
		{
			//sets the given url into a public variable
			throw new System.NotImplementedException();
		}
	}
}

