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
		[UnityTest]
		public IEnumerator TextboxMixcaseTest()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tbActionBar = new ToolboxActionBarVisualElement();
			tbActionBar.Refresh(Provider);

			var search = tbActionBar.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			Debug.Log(text.value);
			var window = text.MountForTest();

			yield return null;

			text.SendTestKeystroke("TeStiNG");

			window.Close();

			Debug.Log(text.value);
			Assert.AreEqual("TeStiNG", text.value);

			model.SetQuery(string.Empty);
		}

		[UnityTest]
		public IEnumerator TextboxLowercaseTest()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tbActionBar = new ToolboxActionBarVisualElement();
			tbActionBar.Refresh(Provider);

			var search = tbActionBar.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			Debug.Log(text.value);
			var window = text.MountForTest();

			yield return null;

			text.SendTestKeystroke("testing");

			window.Close();

			Debug.Log(text.value);
			Assert.AreEqual("testing", text.value);

			model.SetQuery(string.Empty);
		}

		[UnityTest]
		public IEnumerator TextboxUppercaseTest()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tbActionBar = new ToolboxActionBarVisualElement();
			tbActionBar.Refresh(Provider);

			var search = tbActionBar.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			Debug.Log(text.value);
			var window = text.MountForTest();

			yield return null;

			text.SendTestKeystroke("TESTING");

			window.Close();

			Debug.Log(text.value);
			Assert.AreEqual("TESTING", text.value);

			model.SetQuery(string.Empty);
		}
	}
}
