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
		private string _currentCommandId = string.Empty;

		void OnCommandsGui()
		{
			GUILayout.BeginHorizontal();
			OnCommandsScrollView();
			OnCommandsInfo();
			GUILayout.EndHorizontal();
		}

		private void OnCommandsScrollView()
		{
			GUILayout.BeginVertical();
			GUILayout.Box("Commands:");
			DrawVirtualScroller(50, _history.commands.Count, ref _commandsScrollPosition, (index, position) =>
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

				GUILayout.BeginArea(position);
				if (GUILayout.Button(commandStringData.command, buttonStyle))
				{
					delayedActions.Add(() =>
					{
						OnCommandSelected(command.id);
					});
				}
				GUILayout.EndArea();

				GUI.color = defaultColor;

				GUILayout.Space(5);
			});
			GUILayout.EndVertical();
		}

		private void OnCommandsInfo()
		{
			var command = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));

			if (command == null)
			{
				return;
			}

			var commandData = ParseCommandString(command.commandString);
			DrawJsonBlock(commandData.arguments);
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
				if (!commandStringData.arguments.ContainsKey(key))
				{
					commandStringData.arguments.Add(key, value);
				}
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
