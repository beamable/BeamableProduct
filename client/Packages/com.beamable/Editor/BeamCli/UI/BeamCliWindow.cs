using Beamable.Common;
using Beamable.Editor.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.BeamCli.Commands;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Beamable.Editor.BeamCli.UI
{
	public enum BeamCliWindowTab
	{
		Commands,
		Servers,
		Terminal,
		Overrides,
		OTEL
	}
	
	public partial class BeamCliWindow : BeamEditorWindow<BeamCliWindow>
	{
		private BeamWebCliCommandHistory _history;

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/" +
			Constants.Commons.OPEN + " " +
			"CLI Debugger",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async Task Init() => await GetFullyInitializedWindow();
 		
		static BeamCliWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "CLI Debugger",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = false,
			};
		}
		
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/" +
			Constants.Commons.OPEN + " " +
			"CLI Workspace Folder",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static void OpenCliFolderInFileManager()
		{
			BeamEditorContext.Default.Cli.Config(new ConfigArgs()).OnStreamConfigCommandResult(data =>
			{
				var configPath = data.data.configPath;
				var folderPath = Path.GetDirectoryName(configPath);
				Debug.Log($"config path: {configPath}");
				Debug.Log($"folder path: {folderPath}");

				Application.OpenURL($"file://{folderPath}");
			}).OnError(data =>
			{
				Debug.LogError($"{data.data.message}");
			}).Run();
		}
		
		
		// serialized state gets remembered between domain reloads...
		[SerializeField]
		public BeamCliWindowTab selectedTab;

		private float lastTick;

		protected override void Build()
		{
			// nothing to be done...
		}

		public override void OnEnable()
		{
			base.OnEnable();
			lastTick = Time.realtimeSinceStartup;
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			if (selectedTab != BeamCliWindowTab.Commands)
			{
				return;
			}

			float diff = Time.realtimeSinceStartup - lastTick;

			if (diff >= 1.0f)
			{
				lastTick = Time.realtimeSinceStartup;
				Repaint();
			}
		}

		protected override void DrawGUI()
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
			RunDelayedActions();
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
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				selectedTab = (BeamCliWindowTab)GUILayout.Toolbar((int)selectedTab, Enum.GetNames(typeof(BeamCliWindowTab)));
			} 
			GUILayout.EndHorizontal();

			switch (selectedTab)
			{
				case BeamCliWindowTab.Commands:
					OnCommandsGui();
					break;
				case BeamCliWindowTab.Servers:
					OnServerGui();
					break;
				case BeamCliWindowTab.Terminal:
					OnTerminalGui();
					break;
				case BeamCliWindowTab.Overrides:
					OnOverridesGui();
					break;
				case BeamCliWindowTab.OTEL:
					OnOtelGui();
					break;
				default:
					GUILayout.Label("There is no tab implemented yet!");
					break;
			}
		}
	}
}
