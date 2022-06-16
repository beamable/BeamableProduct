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

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Tests.Toolbox
{
    public class ToolboxTests
    {
		public IDependencyProvider provider;

		//Test if ticking filter in tags will change the search bar value to tag:{tag}
		[Test]
		public void TestQueryTag()
		{
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<IToolboxViewService, MockToolboxViewService>();

			provider = builder.Build();

			IToolboxViewService model = provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tb = new ToolboxActionBarVisualElement();
			tb.Refresh(provider);

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

		//Test how many widgets in toolbox appears when NOT filtered
		[Test]
		public void TestEmptyFilterToolbox()
		{
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<IToolboxViewService, MockToolboxViewService>();

			provider = builder.Build();

			IToolboxViewService model = provider.GetService<IToolboxViewService>();

			ToolboxContentListVisualElement toolboxContent = new ToolboxContentListVisualElement();
			toolboxContent.Refresh(provider);

			var x = model.GetFilteredWidgets().Count();
			var filter = toolboxContent.Q("gridContainer");
			var cnt = filter.childCount;

			Debug.Log(cnt);
			Assert.AreEqual(10, cnt);
		}

		//Test if dropdown visual elements pops up
		[Test]
		public void TestCategoryDropdownVisualElement()
		{
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<IToolboxViewService, MockToolboxViewService>();

			provider = builder.Build();

			IToolboxViewService model = provider.GetService<IToolboxViewService>();

			ToolboxActionBarVisualElement tb = new ToolboxActionBarVisualElement();
			tb.Refresh(provider);

			Button button = tb.Q<Button>("CategoryButton");

			CategoryDropdownVisualElement content = new CategoryDropdownVisualElement();
			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(button.worldBound);
			content.Model = model;
			var wnd = BeamablePopupWindow.ShowDropdown("Tags", popupWindowRect, new Vector2(200, 250), content);


			Assert.NotNull(wnd);
		}
	}
}
