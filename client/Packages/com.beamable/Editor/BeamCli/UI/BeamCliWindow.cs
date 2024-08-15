using Beamable.Common;
using Beamable.Editor.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public enum BeamCliWindowTab
	{
		Commands,
		Stats,
		Servers,
		Terminal,
		Overrides
	}
	
	public partial class BeamCliWindow : BeamEditorWindow<BeamCliWindow>
	{
		private BeamWebCliCommandHistory _history;

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"beam cli debug",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async Task Init() => await GetFullyInitializedWindow();

		// serialized state gets remembered between domain reloads...
		[SerializeField]
		public BeamCliWindowTab selectedTab;
		
		[NonSerialized]
		private List<Action> delayedActions = new List<Action>();

		protected override void Build()
		{
			// nothing to be done...
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

			
			// run the actions at the end of the GUI loop, so that all GUI tags are closed.
			foreach (var evt in delayedActions)
			{
				evt?.Invoke();
			}
			delayedActions.Clear();
		}

		void OnNoContextGUI()
		{
			GUILayout.Label("No context... (loading)");
		}
		
		void OnNoHistoryGUI()
		{
			GUILayout.Label("No history... (broken?)");
		}

		void DrawTabButton(BeamCliWindowTab tab, string display)
		{
			var selectedTabStyle = new GUIStyle(EditorStyles.toolbarButton);
			selectedTabStyle.normal.textColor = Color.cyan;
			var tabStyle = EditorStyles.toolbarButton;

			if (GUILayout.Button(display, selectedTab == tab ? selectedTabStyle : tabStyle))
			{
				selectedTab = tab;
			}
		}

		void MainTabsGui()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				DrawTabButton(BeamCliWindowTab.Commands, "Commands");
				DrawTabButton(BeamCliWindowTab.Servers, "Servers");
				DrawTabButton(BeamCliWindowTab.Terminal, "Terminal");
				DrawTabButton(BeamCliWindowTab.Overrides, "Overrides");
				DrawTabButton(BeamCliWindowTab.Stats, "Stats");
			} 
			GUILayout.EndHorizontal();

			GUILayout.Label($"[TEST] tab is {selectedTab}", EditorStyles.miniLabel);

			switch (selectedTab)
			{
				case BeamCliWindowTab.Commands:
					OnCommandsGui();
					break;
				case BeamCliWindowTab.Servers:
					OnServerGui();
					break;
				default:
					GUILayout.Label("There is no tab implemented yet!");
					break;
			}
		}
	}
}
