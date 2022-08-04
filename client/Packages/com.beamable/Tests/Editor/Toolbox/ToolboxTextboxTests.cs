using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Beamable.Editor.Tests.Toolbox
{
    public class ToolboxTextboxTests : EditorTest
    {
		//Test if setting search bar to "layout:landscape" will filter toolbox widgets and tick landscape in layout dropdown
		[UnityTest]
		public IEnumerator KeystrokeTest()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tbActionBar = new ToolboxActionBarVisualElement();
			tbActionBar.Refresh(Provider);

			var search = tbActionBar.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			//search.SetValueWithoutNotify("layout:landscape");
			//text.SetValueWithoutNotify("S");
			Debug.Log(text.value);
			var window = text.MountForTest();

			yield return null;

			text.SendTestKeystroke('a');
			/*foreach (var item in text.SendTestKeyStrokeCoroutine('S'))
			{
				yield return item;
			}*/
			//text.SendTestKeyStrokeCoroutine('S');
			window.Close();

			Assert.AreEqual("a", text.value);

			model.SetQuery(string.Empty);
		}
	}
}
