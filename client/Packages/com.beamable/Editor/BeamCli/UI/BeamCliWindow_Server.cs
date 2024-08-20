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
		public LogView serverLogs;
		public LogView serverEvents;
		
		[NonSerialized]
		public EditorGUISplitView serverSplitter;
		
		[NonSerialized]
		public CliLogDataProvider serverLogProvider;
		[NonSerialized]
		public ServerEventLogDataProvider serverEventsProvider;
		
		void OnServerGui()
		{
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
			

			{
				if (serverSplitter == null)
				{
					serverSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
					if (serverSplitterValue < .01f)
					{
						serverSplitterValue = .2f;
					}
					serverSplitter.splitNormalizedPosition = serverSplitterValue;
				}
			
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
