using Beamable.Common;
using Beamable.Editor.Microservice.UI2.PublishWindow;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2 : BeamEditorWindow<UsamWindow2>
	{
		public UsamService usam;

		public CardButton selectedCard;


		static UsamWindow2()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Beam Services",
				FocusOnShow = false,
				DockPreferenceTypeName = inspector.AssemblyQualifiedName,
				RequireLoggedUser = true,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Services %q",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();


		protected override void Build()
		{
			usam = ActiveContext.ServiceScope.GetService<UsamService>();
			usam.Reload();
		}

		private void OnInspectorUpdate()
		{
			Repaint();
		}

		protected override void DrawGUI()
		{
			if (usam == null)
			{
				DrawBlockLoading(null);
				AddDelayedAction(Build);
				return;
			}

			PrepareCards();
			selectedCard = cards.FirstOrDefault(x => x.name == selectedBeamoId);

			if (UsamPublishWindow.instanceCount > 0)
			{
				GUI.enabled = false;
			}
			DrawMain();
			GUI.enabled = true;

			RunDelayedActions();
		}

	}
}
