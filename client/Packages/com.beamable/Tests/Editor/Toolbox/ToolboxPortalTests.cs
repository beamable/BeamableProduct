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
    public class ToolboxPortalTests : EditorTest
	{
		protected override void Configure(IDependencyBuilder builder)
		{
			builder.ReplaceSingleton<IWebsiteHook, MockWebsiteHook>();
		}

		// A Test behaves as an ordinary method
		[UnityTest]
        public IEnumerator ToolboxPortalTestsSimplePasses()
        {
			IWebsiteHook websiteHook = Provider.GetService<IWebsiteHook>();

			ToolboxBreadcrumbsVisualElement tbBreadcrumbs = new ToolboxBreadcrumbsVisualElement();
			tbBreadcrumbs.Refresh(Provider);

			var portalButton = tbBreadcrumbs.Q<Button>("openPortalButton");

			var window = portalButton.MountForTest();

			yield return null;

			portalButton.SendTestClick();
			window.Close();

			Debug.Log(websiteHook.Url);

			var de = BeamEditorContext.Default;
			string url = $"{BeamableEnvironment.PortalUrl}/{de.CurrentCustomer.Cid}/games/{de.ProductionRealm.Pid}/realms/{de.CurrentRealm.Pid}/dashboard?refresh_token={de.Requester.Token.RefreshToken}";

			Assert.AreEqual(url, websiteHook.Url);
		}
	}
}
