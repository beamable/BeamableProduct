using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		Vector2 _commandsScrollPosition;
		Vector2 _commandsLogScrollPosition;
		Vector2 _commandsPayloadsScrollPosition;
		Vector2 _commandsErrorsScrollPosition;
		private string _currentCommandId = string.Empty;
		public LogView commandLogs;

		public float commandSplitterValue;

		[NonSerialized]
		public CliLogDataProvider commandsLogProvider;

		[NonSerialized]
		public EditorGUISplitView commandSplitter;

		private readonly Dictionary<string, string> commandsStatusIconMap = new Dictionary<string, string>()
		{
			{"Running", "sv_icon_dot13_sml"}, {"Completed", "sv_icon_dot11_sml"}, {"Error", "sv_icon_dot14_sml"}
		};

		void OnCommandsGui()
		{
			EditorGUILayout.BeginVertical();
			DrawTools(new CliWindowToolAction
			{
				name = "Clear Data",
				onClick = () =>
				{
					_history.commands.Clear();
					_commandsScrollPosition = Vector2.zero;
					_commandsLogScrollPosition = Vector2.zero;
					_commandsPayloadsScrollPosition = Vector2.zero;
					_commandsErrorsScrollPosition = Vector2.zero;
				}
			});

			if (commandSplitter == null)
			{
				commandSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
				if (commandSplitterValue < .01f)
				{
					commandSplitterValue = .2f;
				}
				commandSplitter.splitNormalizedPosition = commandSplitterValue;
			}

			EditorGUILayout.BeginHorizontal();
			commandSplitter.BeginSplitView();
			OnCommandsScrollView();

			EditorGUILayout.Space(10, false);
			commandSplitter.Split(this);
			EditorGUILayout.Space(10, false);

			#region right side split

			OnCommandsInfo();

			OnLogsScrollView();

			#endregion

			commandSplitter.EndSplitView();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private void OnCommandsScrollView()
		{
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			GUILayout.Label("Commands", EditorStyles.boldLabel, new GUILayoutOption[]
			{
				GUILayout.ExpandWidth(false)
			});

			DrawVirtualScroller(20, _history.commands.Count, ref _commandsScrollPosition, (index, pos) =>
			{
				var command = _history.commands[index];
				var commandStringData = ParseCommandString(command.commandString);
				var buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
				buttonStyle.alignment = TextAnchor.MiddleLeft;

				var defaultColor = GUI.color;
				if (command.id.Equals(_currentCommandId))
				{
					GUI.color = Color.cyan;
				}


				var iconKey = GetCommandStatusIconKey(command);
				var texture = EditorGUIUtility.FindTexture(iconKey);

				Rect iconRect = new Rect(pos.position.x, pos.position.y, (int)(pos.width * 0.05), pos.height);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

				Rect buttonRect = new Rect(pos.position.x + (int)(pos.width * 0.05), pos.position.y, pos.width, pos.height - 2);
				if (GUI.Button(buttonRect, commandStringData.command, buttonStyle))
				{
					delayedActions.Add(() =>
					{
						OnCommandSelected(command.id);
					});
				}

				GUI.color = defaultColor;
			}, 500);

			EditorGUILayout.EndVertical();
		}

		private void OnCommandsInfo()
		{
			var command = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));
			string commandString = string.Empty;
			int payloadsCount = 0;
			int errorsCount = 0;
			string startTime = string.Empty;
			string endTime = string.Empty;

			if (command != null)
			{
				commandString = command.commandString;
				payloadsCount = command.payloads.Count;
				errorsCount = command.errors.Count;
				startTime = TimeDisplayUtil.GetLogDisplayTime(command.startTime);
				endTime = command.Status == BeamWebCommandDescriptorStatus.DONE ? TimeDisplayUtil.GetLogDisplayTime(command.endTime) : "...";
			}

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			GUIStyle timeLabelsStyle = new GUIStyle( EditorStyles.boldLabel);
			timeLabelsStyle.richText = true;

			GUILayout.Label($"Start Time = [<color=yellow>{startTime}</color>]", timeLabelsStyle);
			GUILayout.Label($"End Time = [<color=yellow>{endTime}</color>]", timeLabelsStyle);

			GUILayout.Space(10);

			var commandData = ParseCommandString(commandString);
			GUILayout.Label($"Command Arguments: [{commandData.command}]", EditorStyles.boldLabel);
			DrawJsonBlock(commandData.arguments);

			GUILayout.Space(10);

			GUILayout.Label($"Command Payloads ({payloadsCount})", EditorStyles.boldLabel);

			DrawVirtualScroller(40, payloadsCount, ref _commandsPayloadsScrollPosition, (index, pos) =>
			{
				Rect labelRect = new Rect(pos.position, new Vector2((int)(pos.width * 0.8), pos.height));
				EditorGUI.SelectableLabel(labelRect, command?.payloads[index].json);

				Rect buttonRect = new Rect(pos.position.x + (int)(pos.width * 0.9), pos.position.y, (int)(pos.width * 0.1), pos.height - 10);
				if (GUI.Button(buttonRect, "Show"))
				{
					PopupWindow.Show(buttonRect, new BeamCliJsonPopup(command?.payloads[index].json));
				}
			}, 120);

			GUILayout.Space(10);

			GUILayout.Label($"Command Errors ({errorsCount})", EditorStyles.boldLabel);

			DrawVirtualScroller(40, errorsCount, ref _commandsErrorsScrollPosition, (index, pos) =>
			{
				EditorGUI.SelectableLabel(pos, command?.errors[index].message);
			}, 120);

			EditorGUILayout.EndVertical();
		}

		private void OnLogsScrollView()
		{
			var command = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));

			if (command == null)
			{
				return;
			}

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			GUILayout.Label($"Command Logs ({command.logs.Count})", EditorStyles.boldLabel);

			if (commandsLogProvider == null)
			{
				commandsLogProvider = new CliLogDataProvider(command.logs);
			}

			this.DrawLogWindow(commandLogs,
			                   dataList: commandsLogProvider,
			                   onClear: () =>
			                   {
				                   command.logs.Clear();
			                   });

			EditorGUILayout.EndVertical();
		}

		private void OnCommandSelected(string id)
		{
			_currentCommandId = id;
		}

		private CommandStringData ParseCommandString(string commandString)
		{
			var commandStringData = new CommandStringData();
			commandStringData.arguments = new ArrayDict();

			string pattern = @"--(?<key>[a-zA-Z\-]+)=(?<value>\""[^\""]*\""|[^ ]+)";
			string nonMatchedPattern = @"(?<unmatched>[^\s]+)";

			MatchCollection matches = Regex.Matches(commandString, pattern);

			foreach (Match match in matches)
			{
				string key = match.Groups["key"].Value;
				string value = match.Groups["value"].Value;
				commandStringData.arguments.TryAdd(key, value);
			}

			string result = Regex.Replace(commandString, pattern, "").Trim();
			MatchCollection unmatchedMatches = Regex.Matches(result, nonMatchedPattern);

			commandStringData.command = string.Join(" ", unmatchedMatches);

			return commandStringData;
		}

		private string GetCommandStatusIconKey(BeamWebCommandDescriptor command)
		{
			if (command.Status == BeamWebCommandDescriptorStatus.DONE)
			{
				return command.exitCode != 0 ? commandsStatusIconMap["Error"] : commandsStatusIconMap["Completed"];
			}

			return commandsStatusIconMap["Running"];
		}
	}

	[Serializable]
	public class CommandStringData
	{
		public ArrayDict arguments;
		public string command;
	}
}
