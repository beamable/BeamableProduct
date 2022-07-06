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

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Tests.Toolbox
{
	public class ToolboxTests : EditorTest
	{
		protected override void Configure(IDependencyBuilder builder)
		{
			builder.ReplaceSingleton<IToolboxViewService, MockToolboxViewService>();
		}

		//Test if ticking filter in tags will change the search bar value to tag:{tag}
		[Test]
		public void TestQueryTag()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tb = new ToolboxActionBarVisualElement();
			tb.Refresh(Provider);

			Button button = tb.Q<Button>("CategoryButton");
			button.SendEvent(new ContextClickEvent());

			SearchBarVisualElement searchBar = tb.Q<SearchBarVisualElement>();

			CategoryDropdownVisualElement content = new CategoryDropdownVisualElement();

			//List of all tags in widget source
			var listRoot = content.Q<VisualElement>("tagList");

			model.SetQueryTag(WidgetTags.FLOW, true);

			SearchBarVisualElement search = tb.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			Debug.Log(text.value);
			Assert.AreEqual("tag:flow", text.value);
		}
	}
}
