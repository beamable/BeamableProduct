using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//using UnityEngine.UI;

using Beamable.Common.Dependencies;
using Beamable.Editor.Tests;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.Components;
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

	/*[BeamContextSystem]
	public class CustomDependencyRegistration
	{
		[RegisterBeamableDependencies()]
		public static void Build(IDependencyBuilder builder)
		{
			builder.RemoveIfExists<IToolboxViewService>();
			builder.AddSingleton<IToolboxViewService, GenerateMockData>(() => GenerateMockData.Instance);
		}
	}*/

	public class MToolboxTests
    {
		public IDependencyProvider provider;
        // A Test behaves as an ordinary method
        /*[Test]
        public void ToolboxViewServiceChecks()
        {
			// Use the Assert class to test conditions

			IToolboxViewService Model = provider.GetService<IToolboxViewService>();

			//_model = ActiveContext.ServiceScope.GetService<IToolboxViewService>();

			//GenerateMockData v = new GenerateMockData();
			//int n = GenerateMockData.Instance.DoSomething(5);

			//int x = 5;
			//Assert.AreEqual(x, 5);
			Assert.IsNotNull(Model);
        }

		[Test]
		public void ToolboxViewServiceSearchQueryFilterText()
		{
			// Use the Assert class to test conditions
			GenerateMockData v = new GenerateMockData();

			v.SetQuery("Admin");
			string filter = v.FilterText;

			//int x = 5;
			Assert.AreEqual("Admin", filter);
		}*/

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
			//Change tag in CategoryDropdownList
			/*
			CategoryDropdownList.doSomething();
			 */
			CategoryDropdownVisualElement content = new CategoryDropdownVisualElement();
			//content.Refresh(provider);
			//Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(button.worldBound);
			//content.Model = tb.Model;
			//var wnd = BeamablePopupWindow.ShowDropdown("Tags", popupWindowRect, new Vector2(200, 250), content);
			//content.Refresh();
			var listRoot = content.Q<VisualElement>("tagList");

			model.SetQueryTag(WidgetTags.FLOW, true);

			SearchBarVisualElement search = tb.Q<SearchBarVisualElement>();
			TextField text = search.Q<TextField>();

			Debug.Log(text.value);
			Assert.AreEqual(text.value, "tag:flow");
		}

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

			//button.SendEvent(new ContextClickEvent());

			CategoryDropdownVisualElement content = new CategoryDropdownVisualElement();
			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(button.worldBound);
			content.Model = model;
			var wnd = BeamablePopupWindow.ShowDropdown("Tags", popupWindowRect, new Vector2(200, 250), content);
			//content.Refresh();


			Assert.NotNull(wnd);
		}
		// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
		// `yield return null;` to skip a frame.
		/*[UnityTest]
        public IEnumerator MToolboxTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }*/
	}
}
