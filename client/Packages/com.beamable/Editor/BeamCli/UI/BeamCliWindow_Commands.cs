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
		Vector2 _commandsPayloadsScrollPosition;
		Vector2 _commandsErrorsScrollPosition;
		private string _currentCommandId = string.Empty;
		public LogView commandLogs = new LogView();

		public float commandSplitterValue;
		public bool isCommandsScrollTailing = true;

		[NonSerialized]
		public CliLogDataProvider commandsLogProvider;

		public EditorGUISplitView commandSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, .2f, .3f, .5f);

		private readonly Dictionary<string, string> commandsStatusIconMap = new Dictionary<string, string>()
		{
			{"Running", "sv_icon_dot13_sml"},
			{"Completed", "sv_icon_dot11_sml"},
			{"Error", "sv_icon_dot14_sml"},
			{"Lost", "sv_icon_dot15_sml"}
		};

		private BeamWebCommandDescriptor selectedCommand;

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
					_commandsPayloadsScrollPosition = Vector2.zero;
					_commandsErrorsScrollPosition = Vector2.zero;
				}
			});

			EditorGUILayout.BeginHorizontal();
			commandSplitter.BeginSplitView();
			OnCommandsScrollView();

			EditorGUILayout.Space(10, false);
			commandSplitter.Split(this);
			EditorGUILayout.Space(10, false);

			#region right side split

			OnCommandsInfo();

			EditorGUILayout.Space(10, false);
			commandSplitter.Split(this);
			EditorGUILayout.Space(10, false);

			
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

			var scrollRectHeight = 500;
			var elementHeight = 20;
			var maxScroll = _history.commands.Count * elementHeight - scrollRectHeight;

			if (isCommandsScrollTailing)
			{
				_commandsScrollPosition.y = maxScroll;
			}
			var startScrollPosition = _commandsScrollPosition.y;

			DrawVirtualScroller(elementHeight, _history.commands.Count, ref _commandsScrollPosition, (index, pos) =>
			{
				var command = _history.commands[index];
				var commandStringData = ParseCommandString(command.commandString);
				var buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
				buttonStyle.alignment = TextAnchor.MiddleLeft;

				var iconKey = GetCommandStatusIconKey(command);
				var texture = EditorGUIUtility.FindTexture(iconKey);

				Rect iconRect = new Rect(pos.position.x, pos.position.y, (int)(pos.width * 0.05), pos.height);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

				var defaultColor = GUI.color;
				if (command.id.Equals(_currentCommandId))
				{
					GUI.color = Color.cyan;
				}

				Rect buttonRect = new Rect(pos.position.x + (int)(pos.width * 0.05), pos.position.y, pos.width, pos.height - 2);
				var displayTimeText = TimeDisplayUtil.GetLogDisplayTime(command.createdTime);
				var commandText = $"[{displayTimeText}] {commandStringData.command}";
				if (GUI.Button(buttonRect, commandText, buttonStyle))
				{
					delayedActions.Add(() =>
					{
						OnCommandSelected(command.id);
					});
				}

				GUI.color = defaultColor;
			}, scrollRectHeight);

			// if the user tries to interact with the scroll, then un-tail
			if (Math.Abs(startScrollPosition - _commandsScrollPosition.y) > .0001f)
			{
				isCommandsScrollTailing = false;
			}

			// if the user is close enough to the end, then become tail
			if (Math.Abs(maxScroll - _commandsScrollPosition.y) < .01f)
			{
				isCommandsScrollTailing = true;
			}

			EditorGUILayout.EndVertical();
		}

		private void OnCommandsInfo()
		{
			var command = GetSelectedCommand();

			string commandString = string.Empty;
			int payloadsCount = 0;
			int errorsCount = 0;
			string startTime = string.Empty;
			string endTime = string.Empty;
			string elapsedTimeText = string.Empty;
			string timeFormat = "HH:mm:ss.ffff";

			if (command != null)
			{
				commandString = command.commandString;
				payloadsCount = command.payloads.Count;
				errorsCount = command.errors.Count;
				startTime = command.startTime > 0 ? TimeDisplayUtil.GetLogDisplayTime(command.startTime, timeFormat) : "...";
				endTime = command.Status == BeamWebCommandDescriptorStatus.DONE ? TimeDisplayUtil.GetLogDisplayTime(command.endTime, timeFormat) : "...";
				elapsedTimeText = GetElapsedTime(command);
			}

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			GUIStyle timeLabelsStyle = new GUIStyle( EditorStyles.boldLabel);
			timeLabelsStyle.richText = true;

			GUILayout.Label($"Start Time = [<color=yellow>{startTime}</color>]", timeLabelsStyle);
			GUILayout.Label($"End Time = [<color=yellow>{endTime}</color>]", timeLabelsStyle);
			GUILayout.Label($"Elapsed Time = [<color=yellow>{elapsedTimeText}</color>]", timeLabelsStyle);

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
				Rect labelRect = new Rect(pos.position.x, pos.position.y, (int)(pos.width * 0.75), pos.height);
				EditorGUI.SelectableLabel(labelRect, command?.errors[index].message);

				float buttonWidth = (int)(pos.width * 0.2);
				float xAtEnd = pos.position.x + (pos.width - buttonWidth - 10);
				Rect buttonRect = new Rect(xAtEnd, pos.position.y + (int)(pos.height * 0.25), buttonWidth, (int)(pos.height * 0.5));
				if (GUI.Button(buttonRect, "Stack Trace"))
				{
					PopupWindow.Show(buttonRect, new BeamCliTextPopup("Stack Trace:", command?.errors[index].stackTrace));
				}
			}, 120);

			EditorGUILayout.EndVertical();
		}

		private void OnLogsScrollView()
		{
			var command = GetSelectedCommand();
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
			GetSelectedCommand(true);
		}

		BeamWebCommandDescriptor GetSelectedCommand(bool force=false)
		{
			if (force || selectedCommand == null)
			{
				selectedCommand = _history.commands.FirstOrDefault(c => c.id.Equals(_currentCommandId));
				if (selectedCommand == null)
				{
					commandsLogProvider = new CliLogDataProvider(new List<CliLogMessage>());
				}
				else
				{
					commandsLogProvider = new CliLogDataProvider(selectedCommand.logs);
				}

				commandLogs.BuildView(commandsLogProvider, true);
				return selectedCommand;
			}
			else
			{
				return selectedCommand;
			}
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
				commandStringData.arguments.AddUnchecked(key, value);
			}

			string result = Regex.Replace(commandString, pattern, "").Trim();
			MatchCollection unmatchedMatches = Regex.Matches(result, nonMatchedPattern);
			var unmatchedMatchResults = new string[unmatchedMatches.Count];
			for (var i = 0; i < unmatchedMatchResults.Length; i++)
			{
				unmatchedMatchResults[i] = unmatchedMatches[i].Value;
			}
			commandStringData.command = string.Join(" ", unmatchedMatchResults);

			return commandStringData;
		}

		private string GetCommandStatusIconKey(BeamWebCommandDescriptor command)
		{
			if (command.instance == null)
			{
				return commandsStatusIconMap["Lost"];
			}

			if (command.Status == BeamWebCommandDescriptorStatus.DONE)
			{
				return command.errors.Count > 0 ? commandsStatusIconMap["Error"] : commandsStatusIconMap["Completed"];
			}

			return commandsStatusIconMap["Running"];
		}

		private string GetElapsedTime(BeamWebCommandDescriptor command)
		{
			TimeSpan diff;
			if (command.Status == BeamWebCommandDescriptorStatus.DONE)
			{
				diff = DateTime.FromFileTime(command.endTime) - DateTime.FromFileTime(command.startTime);
				return  $"{diff.Hours:00}:{diff.Minutes:00}:{diff.Seconds:00}.{diff.Milliseconds:0000}";
			}

			if (command.startTime <= 0)
			{
				return "00:00:00.0000";
			}

			diff = DateTime.Now - DateTime.FromFileTime(command.startTime);
			return $"{diff.Hours:00}:{diff.Minutes:00}:{diff.Seconds:00}.{diff.Milliseconds:0000}";
		}
	}

	[Serializable]
	public class CommandStringData
	{
		public ArrayDict arguments;
		public string command;
	}
}
