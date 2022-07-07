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
			Assert.AreEqual("layout:landscape", search.Value);
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

			var w = model.GetFilteredWidgets();

			foreach (var i in w)
			{
				Debug.Log(i.Name);
			}

			var filter = toolboxContent.Q("gridContainer");
			var cnt = filter.childCount;

			Assert.AreEqual(8, cnt);
		}
	}
}
