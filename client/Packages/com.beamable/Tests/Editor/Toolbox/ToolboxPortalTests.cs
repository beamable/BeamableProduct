using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEditor.EventSystems;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Tests.Toolbox
{
	public class FML : MonoBehaviour
	{
		//idk 
		static void ExecuteEvent(Button button)
		{
			Button b = button;
			GameObject g = new GameObject();
			

			g.AddComponent(typeof(Button));
			g.GetComponent<Button>().Equals(button);

			//ExecuteEvents.Execute(g.gameObject, new BaseEventData(eventSystem), )
		}
	}
    public class ToolboxPortalTests : EditorTest
	{
		protected override void Configure(IDependencyBuilder builder)
		{
			builder.ReplaceSingleton<IWebsiteHook, MockWebsiteHook>();
		}

		// A Test behaves as an ordinary method
		[Test]
        public void ToolboxPortalTestsSimplePasses()
        {
			var de = BeamEditorContext.Default;
			//string url = $"{BeamableEnvironment.PortalUrl}/{de.CurrentCustomer.Cid}/games/{de.ProductionRealm.Pid}/realms/{de.CurrentRealm.Pid}/dashboard?refresh_token={de.Requester.Token.RefreshToken}";
			
			IWebsiteHook websiteHook = Provider.GetService<IWebsiteHook>();

			ToolboxBreadcrumbsVisualElement tbBreadcrumbs = new ToolboxBreadcrumbsVisualElement();
			tbBreadcrumbs.Refresh(Provider);

			//emit a click on this button
			var portalButton = tbBreadcrumbs.Q<Button>("openPortalButton");
			//portalButton.SendEvent(new MouseMoveEvent());
			//portalButton.SendEvent(new MouseUpEvent());
			//portalButton.clickable.activators.Clear();
			//portalButton.clickable.SendEvent();
			//portalButton.SendEvent(new Clickable(new ContextClickEvent()));

			//Clickable click = portalButton.clickable;
			//portalButton.HandleEvent(new ContextLeftClickEvent());
			//portalButton.SendEvent(new ContextLeftClickEvent());

			using (var e = new ContextLeftClickEvent() { target = portalButton })
				portalButton.SendEvent(e);

			portalButton.RegisterCallback<MouseUpEvent>((evt) => Debug.Log("TEST"));

			//portalButton.clickable.clicked += () => Debug.Log("PWP");

			//portalButton.RegisterCallback<MouseDownEvent>((MouseDownEvent evt) => Debug.Log("test"), TrickleDown.TrickleDown);

			//temp only
			websiteHook.OpenUrl("trouble in errorist town");
			Debug.Log(websiteHook.Url);

			//var de = BeamEditorContext.Default;
			Assert.AreEqual($"{BeamableEnvironment.PortalUrl}/{de.CurrentCustomer.Cid}/games/{de.ProductionRealm.Pid}/realms/{de.CurrentRealm.Pid}/dashboard?refresh_token={de.Requester.Token.RefreshToken}", websiteHook.Url);
		}
	}
}
