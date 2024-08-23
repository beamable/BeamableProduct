using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ThirdParty.Splitter;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		// public Vector2 scrollerPositionServerLogs;
		public Vector2 scrollerPositionServerEvents;
		// public Vector2 serverLogsSelectedIndexScroll;

		// public int selectedIndex = -1;

		// public bool verboseToggled;

		public float serverSplitterValue;
		public LogView serverLogs = new LogView();
		public LogView serverEvents = new LogView();
		public Vector2 serverCliScroll;
		
		public EditorGUISplitView serverSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, .2f, .8f);
		
		[NonSerialized]
		public CliLogDataProvider serverLogProvider;
		[NonSerialized]
		public ServerEventLogDataProvider serverEventsProvider;
		
		void OnServerGui()
		{
			var service = ActiveContext.ServiceScope.GetService<BeamWebCommandFactory>();
			var process = service.GetServerProcess();
			DrawTools(new CliWindowToolAction
			{
				name = "Clear Data",
				onClick = () =>
				{
					_history.serverEvents.Clear();
					_history.serverLogs.Clear();
					
					serverLogs.BuildView(serverLogProvider, true);
					serverEvents.BuildView(serverEventsProvider, true);
				}
			}, new CliWindowToolAction
			{
				name = "Kill Server",
				onClick = () =>
				{
					Debug.Log("killing server...");
					service.KillServer();
				}
			});
			
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Server Ping", EditorStyles.boldLabel, new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(false)
				});
				
				GUILayout.Label("(readonly)", EditorStyles.miniLabel, new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(true),
				});
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				// GUI.enabled = false;
				EditorGUI.indentLevel++;
				EditorGUILayout.TextField("status", _history.latestPing.result.ToString());
				
				DrawJsonBlock(_history.latestPing);
				
				// GUI.enabled = true;
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
			
			
			EditorGUILayout.BeginVertical();
			var hasProcess = !string.IsNullOrEmpty(process);
			
			if (hasProcess)
			{
				EditorGUILayout.LabelField("Running local server", EditorStyles.boldLabel);
				DrawScrollableSelectableTextBox(process, ref serverCliScroll, 70, EditorStyles.helpBox);
			}
			else
			{
				EditorGUILayout.LabelField("Local server is not running (maybe connected to other server)", EditorStyles.boldLabel);
			}

			EditorGUILayout.EndVertical();


			{
				
				EditorGUILayout.BeginHorizontal();
				serverSplitter.BeginSplitView();

				{
					EditorGUILayout.BeginVertical();
					if (serverEventsProvider == null)
					{
						serverEventsProvider = new ServerEventLogDataProvider(_history.serverEvents);
					}

					GUILayout.Label($"Server Events ({_history.serverEvents.Count})", EditorStyles.boldLabel);

					this.DrawLogWindow(serverEvents,
					                   dataList: serverEventsProvider,
					                   onClear: () =>
					                   {
						                   _history.serverEvents.Clear();
					                   });
					EditorGUILayout.EndVertical();
				}
				
				
				EditorGUILayout.Space(15, false);
				serverSplitter.Split(this);
				EditorGUILayout.Space(5, false);

				
				{
					EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					GUILayout.Label($"Server Logs ({_history.serverLogs.Count})", EditorStyles.boldLabel);

					if (serverLogProvider == null)
					{
						serverLogProvider = new CliLogDataProvider(_history.serverLogs);
					}

					this.DrawLogWindow(serverLogs,
					                   dataList: serverLogProvider,
					                   onClear: () =>
					                   {
						                   _history.serverLogs.Clear();
					                   });
					EditorGUILayout.EndVertical();
				}

				serverSplitter.EndSplitView();
				EditorGUILayout.EndHorizontal();
				

			}

		}
	}
}
