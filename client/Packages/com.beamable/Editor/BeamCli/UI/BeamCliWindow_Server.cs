using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		public Vector2 scrollerPositionServerLogs;
		public Vector2 scrollerPositionServerEvents;
		
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
			

			GUILayout.Label("Server Data ", EditorStyles.boldLabel);
			{
				DrawTools(new CliWindowToolAction
				{
					name = "clear",
					onClick = () =>
					{
						_history.serverEvents.Clear();
						_history.serverLogs.Clear();
						scrollerPositionServerEvents = Vector2.zero;
					}
				});

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical(GUILayout.Width(250));
				GUILayout.Label($"Server Events ({_history.serverEvents.Count})", EditorStyles.boldLabel);
				DrawVirtualScroller(30, _history.serverEvents.Count, ref scrollerPositionServerEvents, (index, position) =>
				{
					EditorGUI.SelectableLabel(position, _history.serverEvents[index].message);
				});
				EditorGUILayout.EndVertical();

				EditorGUILayout.Space(30, false);
				
				EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				GUILayout.Label($"Server Logs ({_history.serverLogs.Count})", EditorStyles.boldLabel);

				DrawVirtualScroller(40, _history.serverLogs.Count, ref scrollerPositionServerLogs, (index, position) =>
				{
					EditorGUI.SelectableLabel(position, _history.serverLogs[index].message.message);
				});
				EditorGUILayout.EndVertical();

				
				EditorGUILayout.EndHorizontal();
				

			}

		}
	}
}
