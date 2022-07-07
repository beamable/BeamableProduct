using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using Beamable.Editor.Content.Components;

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

		//Test if setting search bar to "layout:landscape" will filter toolbox widgets and tick landscape in layout dropdown
		[Test]
		public void LayoutSearchbarLandscapeTest()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tbActionBar = new ToolboxActionBarVisualElement();
			tbActionBar.Refresh(Provider);

			var search = tbActionBar.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			search.SetValueWithoutNotify("layout:landscape");

			//set Query to newly set search value
			model.SetQuery(search.Value);

			var x = model.GetFilteredWidgets();

			TypeDropdownVisualElement typeDropdown = new TypeDropdownVisualElement();
			typeDropdown.Model = model;
			typeDropdown.Refresh();

			var drp = typeDropdown.Q<VisualElement>("typeList");

			foreach (var i in drp.Children())
			{
				Debug.Log(i.Q<Label>().text + " : " + i.Q<Toggle>().value);
			}

			Debug.Log("Number of Widgets: " + x.Count());
			Debug.Log(search.Value);

			Assert.AreEqual(true, drp[1].Q<Toggle>().value);
			Assert.AreEqual("layout:landscape", text.value);
			Assert.AreEqual(4, x.Count());
		}

		//Test if tag filter toolbox widgets
		[Test]
		public void TestFilterTagToolbox()
		{
			IToolboxViewService model = Provider.GetService<IToolboxViewService>();

			ToolboxContentListVisualElement toolboxContent = new ToolboxContentListVisualElement();
			toolboxContent.Refresh(Provider);

			model.SetQueryTag(WidgetTags.FLOW, true);

			var x = model.GetFilteredWidgets().Count();

			var filter = toolboxContent.Q("gridContainer");
			var cnt = filter.childCount;

			Assert.AreEqual(8, x);
		}
	}
}
