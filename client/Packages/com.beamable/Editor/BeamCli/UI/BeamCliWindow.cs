using Beamable.Common;
using Beamable.Editor.UI;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public class BeamCliWindow : BeamEditorWindow<BeamCliWindow>
	{
		private BeamWebCliCommandHistory _history;

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"beam cli debug",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async Task Init() => await GetFullyInitializedWindow();
		
		protected override void Build()
		{
			
		}

		private void OnGUI()
		{
			if (ActiveContext == null)
			{
				OnNoContextGUI();
				return;
			}

			_history = ActiveContext.ServiceScope.GetService<BeamWebCliCommandHistory>();
			if (_history == null)
			{
				OnNoHistoryGUI();
				return;
			}

			MainTabsGui();
		}

		void OnNoContextGUI()
		{
			GUILayout.Label("No context... (loading)");
		}
		
		void OnNoHistoryGUI()
		{
			GUILayout.Label("No history... (broken?)");
		}

		void MainTabsGui()
		{
			GUILayout.Label($"[TEST] There are {_history.commands.Count} commands...");
		}
	}
}
