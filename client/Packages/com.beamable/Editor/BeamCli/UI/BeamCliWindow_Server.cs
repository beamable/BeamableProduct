using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
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
			

			GUILayout.Label("Server Event Stream", EditorStyles.boldLabel);
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

				DrawVirtualScroller(40, _history.serverLogs.Count, ref scrollerPositionServerEvents, (index, position) =>
				{
					EditorGUI.LabelField(position, _history.serverLogs[index].message.message);
					// GUILayout.Label("log number: " + index, GUILayout.Height(position.height));
				});

				// scrollerPositionServerEvents = GUILayout.BeginScrollView(scrollerPositionServerEvents);
				// {
				// 	for (var i = 0; i < _history.serverLogs.Count; i++)
				// 	{
				// 		var evt = _history.serverLogs[i];
				// 		GUILayout.Label($"[{evt.message.logLevel}] {evt.message.message}");
				// 	}
				// }
				// GUILayout.EndScrollView();
			}

		}
	}
}
