using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		public string terminalInput;

		public CliLogDataProvider logProvider;
		public LogView terminalLogView;
		public List<CliJsonBlockData> terminalResults = new List<CliJsonBlockData>();
		public List<CliJsonBlockData> terminalErrors = new List<CliJsonBlockData>();
		public Vector2 terminalErrorScroller;
		public Vector2 terminalResultScroller;

		public float terminalSplitValue = .5f;
		[NonSerialized]
		public EditorGUISplitView terminalSplitter;

		
		[NonSerialized]
		private Promise _commandPromise;
		
		void OnTerminalGui()
		{
			if (terminalSplitter == null)
			{
				terminalSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
				terminalSplitter.splitNormalizedPosition = terminalSplitValue;
				
			}
			{
				
				var style = new GUIStyle(EditorStyles.label)
				{
					wordWrap = true, 
					padding = new RectOffset(2, 2, 2, 2)
				};
				
				// draw the log area..
				// EditorGUILayout.SelectableLabel("blah!\ndop", style, GUILayout.ExpandHeight(true));

				if (logProvider == null)
				{
					logProvider = new CliLogDataProvider(new List<CliLogMessage>());
				}

				EditorGUILayout.BeginHorizontal();
				terminalSplitter.BeginSplitView();
				
				EditorGUILayout.BeginVertical();

				this.DrawLogWindow(terminalLogView,
				                   dataList: logProvider,
				                   onClear: () =>
				                   {
					                   logProvider.data.Clear();
				                   });
				
				EditorGUILayout.EndVertical();

				EditorGUILayout.Space(10, false);
				terminalSplitter.Split(this);
				EditorGUILayout.Space(10, false);

				EditorGUILayout.BeginVertical();

				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.LabelField("Terminal Output", EditorStyles.boldLabel);
					DrawVirtualScroller(200, terminalResults.Count, ref terminalResultScroller, (index, rect) =>
					{
						var json = terminalResults[index];
						DrawJsonBlock(rect, json);
				
					});
				}
				
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.LabelField("Terminal Errors", EditorStyles.boldLabel);
					DrawVirtualScroller(200, terminalErrors.Count, ref terminalErrorScroller, (index, rect) =>
					{
						var json = terminalErrors[index];
						DrawJsonBlock(rect, json);
				
						
					});
				}
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.EndVertical();
				
				
				terminalSplitter.EndSplitView();
				EditorGUILayout.EndHorizontal();
				
				// draw the input area
				
				var inputStyle = GetCodeStyle();
				inputStyle.normal.textColor = new Color(.2f, .9f, .4f);
				inputStyle.hover = inputStyle.active = inputStyle.focused = inputStyle.normal;
				inputStyle.fontSize = 18;
				
				if (!string.IsNullOrEmpty(terminalInput) && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
				{
					RunTerminalCommand();
				}
				
				terminalInput = GUILayout.TextField(terminalInput, inputStyle);

			} 
		}

		void RunTerminalCommand()
		{
			var argStr = terminalInput;
			terminalInput = ""; // clear
			
			var command = (BeamWebCommand)ActiveContext.Cli.CreateCustom(argStr);
			Debug.Log(command.commandString);
			var logs = new List<CliLogMessage>();
			logProvider = new CliLogDataProvider(logs);
			terminalLogView = new LogView();
			terminalErrors.Clear();
			terminalResults.Clear();
			
			command.On(cb =>
			{
				if (cb.type == "logs")
				{
					var msg = JsonUtility.FromJson<ReportDataPoint<CliLogMessage>>(cb.json);
					logs.Add(msg.data);
					terminalLogView.RebuildView();
					
				} 
				else // data payload...
				{
					var dict = (ArrayDict)Json.Deserialize(cb.json);
					var highlightedJson = JsonHighlighterUtil.HighlightJson(dict);

					if (cb.type.StartsWith("error"))
					{
						terminalErrors.Add(new CliJsonBlockData
						{
							highlightedJson = highlightedJson,
							originalJson = cb.json,
						});
					}
					else
					{
						terminalResults.Add(new CliJsonBlockData
						{
							highlightedJson = highlightedJson,
							originalJson = cb.json,
						});
					}
				}
				
				Debug.Log(cb.type + " / " + cb.json);
			});
			command.Run().Then(_ =>
			{
				Debug.Log("DONE");
				this.Repaint();
			});


		}
	}
}
