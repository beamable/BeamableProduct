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
				}
			});

			EditorGUILayout.BeginHorizontal();
			OnCommandsScrollView();
			OnCommandsInfo();
			OnLogsScrollView();
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

			_commandsScrollPosition = EditorGUILayout.BeginScrollView(_commandsScrollPosition, GUILayout.Width(500), GUILayout.Height(500));
			var areaRect = new Rect(_commandsScrollPosition.x, _commandsScrollPosition.y, 500, 500);
			EditorGUI.DrawRect(areaRect, new Color(1,1,1,.1f));

			foreach (var command in _history.commands)
			{
				var commandStringData = ParseCommandString(command.commandString);
				var buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
				buttonStyle.alignment = TextAnchor.MiddleLeft;

				var defaultColor = GUI.color;
				if (command.id.Equals(_currentCommandId))
				{
					GUI.color = Color.cyan;
				}

				if (GUILayout.Button(commandStringData.command, buttonStyle))
				{
					delayedActions.Add(() =>
					{
						OnCommandSelected(command.id);
					});
				}

				GUI.color = defaultColor;

				EditorGUILayout.Space(5);
			}

			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();
		}

		private void OnCommandsInfo()
		{
			var command = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));
			string commandString = string.Empty;
			int payloadsCount = 0;
			int errorsCount = 0;

			if (command != null)
			{
				commandString = command.commandString;
				payloadsCount = command.payloads.Count;
				errorsCount = command.errors.Count;
			}

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			var commandData = ParseCommandString(commandString);
			GUILayout.Label($"Command Arguments: [{commandData.command}]", EditorStyles.boldLabel);
			DrawJsonBlock(commandData.arguments);

			GUILayout.Label($"Command Payloads ({payloadsCount})", EditorStyles.boldLabel);

			DrawVirtualScroller(40, payloadsCount, ref _commandsPayloadsScrollPosition, (index, pos) =>
			{
				EditorGUI.SelectableLabel(pos, command?.payloads[index].json);
			}, 150);

			GUILayout.Label($"Command Errors ({errorsCount})", EditorStyles.boldLabel);


			DrawVirtualScroller(40, errorsCount, ref _commandsErrorsScrollPosition, (index, pos) =>
			{
				EditorGUI.SelectableLabel(pos, command?.errors[index].message);
			}, 150);

			EditorGUILayout.EndVertical();
		}

		private void OnLogsScrollView()
		{
			var command = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));
			int commandLogsCount = 0;

			if (command != null)
			{
				commandLogsCount = command.logs.Count;
			}

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			GUILayout.Label($"Command Logs ({commandLogsCount})", EditorStyles.boldLabel);

			DrawVirtualScroller(40, commandLogsCount, ref _commandsLogScrollPosition, (index, pos) =>
			{
				EditorGUI.SelectableLabel(pos, command?.logs[index].message);
			}, 450);

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
	}

	[Serializable]
	public class CommandStringData
	{
		public ArrayDict arguments;
		public string command;
	}
}
