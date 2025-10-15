using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using Beamable.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI.LogHelpers
{

	[Serializable]
	public class LogLevelView
	{
		public int lastCount;
		public int count;
		public bool enabled = true;
	}
	
	[Serializable]
	public class LogView
	{
		public SearchData _searchData;
		
		public int selectedIndex;
		public Vector2 logScroll;
		public Vector2 selectedScroll;

		public LogLevelView verbose = new LogLevelView();
		public LogLevelView debug = new LogLevelView();
		public LogLevelView info = new LogLevelView();
		public LogLevelView warning = new LogLevelView();
		public LogLevelView error = new LogLevelView();
		public LogLevelView fatal = new LogLevelView();

		public bool isTailing = true;
		

		public bool clearOnPlay = true;
		
		/// <summary>
		/// this variable is used to process information across frames
		/// </summary>
		[NonSerialized]
		public int scanIndex;
		
		[NonSerialized]
		public List<CliLogMessage> view;

		[NonSerialized]
		public LogDataProvider data;

		[NonSerialized]
		public int lastDataCount;

		[NonSerialized]
		public bool initialized;

		[NonSerialized]
		private IDelayedActionWindow _window;

		public string searchText
		{
			get => _searchData.searchText; 
			set => _searchData.searchText = value;
		}
		
		public SearchData searchData => _searchData;

		public LogView()
		{
			_searchData = new SearchData {onEndCheck = RebuildView};
		}

		public void Scan(int budget, out bool foundDiff)
		{
			foundDiff = false;
			if (view == null) return;

			var count = data.Count();
			if (count > lastDataCount)
			{
				var subSet = data.data.Skip(lastDataCount);
				view.AddRange(subSet);
				lastDataCount = count;
				scanIndex = 0; // start from end of log stream.

				verbose.count = 0;
				debug.count = 0;
				info.count = 0;
				error.count = 0;
				fatal.count = 0;
				warning.count = 0;
				foundDiff = true;
			}

			for (var i = 0; i < budget; i++)
			{
				
				var index = (count - scanIndex) - 1;
				if (index < 0 || index >= count)
				{
					// scanIndex = 0;
					break;
				}

				var log = data.data[index];

				switch (log.GetLogLevel())
				{
					case CliLogLevel.Verbose:
						foundDiff = true;
						verbose.count++;

						if (!verbose.enabled)
						{
							view.Remove(log);
						}
						break;
					case CliLogLevel.Debug:
						foundDiff = true;
						debug.count++;

						if (!debug.enabled)
						{
							view.Remove(log);
						}
						break;
					case CliLogLevel.Info:
						foundDiff = true;
						info.count++;

						if (!info.enabled)
						{
							view.Remove(log);
						}
						break;
					case CliLogLevel.Warning:
						foundDiff = true;
						warning.count++;

						if (!warning.enabled)
						{
							view.Remove(log);
						}
						break;
					case CliLogLevel.Error:
						foundDiff = true;
						error.count++;

						if (!error.enabled)
						{
							view.Remove(log);
						}
						break;
					case CliLogLevel.Fatal:
						foundDiff = true;
						fatal.count++;

						if (!fatal.enabled)
						{
							view.Remove(log);
						}
						break;
				}
				
				if (!string.IsNullOrEmpty(searchText) &&
				    !log.message.ToLowerInvariant().Contains(searchText.ToLowerInvariant()))
				{
					foundDiff = true;
					view.Remove(log);
				}
				
				scanIndex++;
			}
		}

		public void Init(IDelayedActionWindow window)
		{
			_window = window;
			if (!initialized)
			{
				EditorApplication.update += Update;
				initialized = true;
				
			}
		}

		public void RebuildView()
		{
			scanIndex = 0;
			verbose.count = 0;
			info.count = 0;
			debug.count = 0;
			warning.count = 0;
			error.count = 0;
			fatal.count = 0;
			view = data.data.ToList(); // copy.
			lastDataCount = data.Count();
		}
		
		public void BuildView(LogDataProvider dataList, bool force = false)
		{
			if (view == null || force)
			{
				data = dataList;
				RebuildView();
			}
		}

		void Update()
		{
			Scan(100, out var diff);
			if (diff)
			{
				_window.Repaint();
			}
			else
			{
				verbose.lastCount = verbose.count;
				debug.lastCount = debug.count;
				info.lastCount = info.count;
				warning.lastCount = warning.count;
				error.lastCount = error.count;
				fatal.lastCount = fatal.count;
			}
		}
	}

	[Serializable]
	public class SearchData
	{
		public string searchText;
		[NonSerialized]
		public Action onEndCheck;
	}
	
	public static class CliLogMessageExtensions
	{
		public static CliLogLevel GetLogLevel(this CliLogMessage log)
		{
			switch (log.logLevel)
			{
				case "Trace":
				case "trace":
				case "Verbose":
				case "verbose":
					return CliLogLevel.Verbose;
				case "Debug":
				case "debug":
					return CliLogLevel.Debug;
				case "Warning":
				case "warning":
				case "warn":
					return CliLogLevel.Warning;
				case "Information":
				case "information":
				case "info":
				case "Info":
					return CliLogLevel.Info;
				case "Fatal":
				case "fatal":
					return CliLogLevel.Fatal;
				case "Error":
				case "error":
					return CliLogLevel.Error;
				default:
					throw new NotSupportedException("unknown log level " + log.logLevel);
			}
		}
	}

	public enum CliLogLevel
	{
		Verbose,
		Debug,
		Info,
		Warning,
		Error,
		Fatal
	}

	public abstract class LogDataProvider
	{
		public bool showLogLevels;
		public abstract List<CliLogMessage> data { get; }
		public abstract int Count();

		public abstract CliLogMessage GetValueAtIndex(int index);
	}

	public class CliLogDataProvider : LogDataProvider
	{
		public CliLogDataProvider(List<CliLogMessage> list)
		{
			data = list;
			showLogLevels = true;
		}
		public override List<CliLogMessage> data { get; }
		public override int Count() => data.Count;
		public override CliLogMessage GetValueAtIndex(int index) => data[index];
	}

	public class ServerEventLogDataProvider : LogDataProvider
	{
		private List<BeamCliServerEvent> _serverEvents;

		private int lastCount = -1;
		private List<CliLogMessage> logMessages = new List<CliLogMessage>();

		public ServerEventLogDataProvider(List<BeamCliServerEvent> serverEvents)
		{
			_serverEvents = serverEvents;
			showLogLevels = false;
		}

		public override List<CliLogMessage> data
		{
			get
			{
				if (lastCount == _serverEvents.Count)
				{
					return logMessages;
				}

				logMessages = _serverEvents.Select(x => new CliLogMessage
				{
					message = x.message, 
					logLevel = "info"
				}).ToList();
				lastCount = logMessages.Count;
				return logMessages;
			}
		}
		public override int Count() => _serverEvents.Count;
		public override CliLogMessage GetValueAtIndex(int index) => data[index];
	}

	public static class LogUtil
	{

		public static void DrawLogWindow(this IDelayedActionWindow window,
		                                 LogView logView,
		                                 LogDataProvider dataList,
		                                 Action onClear,
		                                 Func<LogView, bool> customClearGui = null)
		{

			logView.BuildView(dataList);
			logView.Init(window);

			var labelStyle = new GUIStyle(EditorStyles.label);
			labelStyle.wordWrap = true;
			labelStyle.alignment = TextAnchor.UpperLeft;
			labelStyle.padding = new RectOffset(2, 2, 2, 2);

			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				bool isClear = false;
				if (customClearGui == null)
				{
					isClear = GUILayout.Button("clear", EditorStyles.toolbarButton, GUILayout.Width(50));

				}
				else
				{
					isClear = customClearGui.Invoke(logView);
				}

				if (isClear)
				{
					window.AddDelayedAction(() =>
					{
						onClear();
						logView.BuildView(dataList, true);
					});
				}

				EditorGUILayout.Space(1, false);

				DrawSearchBar(window, logView.searchData);


				// EditorGUILayout.Space(5, false);
				if (dataList.showLogLevels)
				{
					DrawLogLevelToggle(CliLogLevel.Verbose, logView.verbose, window, logView);
					DrawLogLevelToggle(CliLogLevel.Debug, logView.debug, window, logView);
					DrawLogLevelToggle(CliLogLevel.Info, logView.info, window, logView);
					DrawLogLevelToggle(CliLogLevel.Warning, logView.warning, window, logView);
					DrawLogLevelToggle(CliLogLevel.Error, logView.error, window, logView);
					DrawLogLevelToggle(CliLogLevel.Fatal, logView.fatal, window, logView);
				}

				EditorGUILayout.EndHorizontal();
			}

			var scrollRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
			                                          new GUILayoutOption[]
			                                          {
				                                          GUILayout.ExpandHeight(true), GUILayout.MinHeight(100)
			                                          });

			var text = "";
			if (logView.selectedIndex >= 0 && logView.selectedIndex < logView.view.Count)
			{
				text = logView.view[logView.selectedIndex].message;
			}

			BeamCliWindow.DrawScrollableSelectableTextBox(text, ref logView.selectedScroll, 100);

			var elementHeight = (int)EditorGUIUtility.singleLineHeight * 2;
			var maxScroll = logView.view.Count * elementHeight - scrollRect.height;

			if (logView.isTailing)
			{
				logView.logScroll.y = maxScroll;
			}

			var startScrollPosition = logView.logScroll.y;
			BeamCliWindow.DrawVirtualScroller(scrollRect, elementHeight, logView.view.Count, ref logView.logScroll,
			                                  (index, position) =>
			                                  {
				                                  if (index >= logView.view.Count) return false;

				                                  var log = logView.view[index];

				                                  /*
				                                   * progressive filtering will make the log move a bit, but I think its worth it for the speed improvements...
				                                   *
				                                   */
				                                  switch (log.GetLogLevel())
				                                  {
					                                  case CliLogLevel.Verbose:
						                                  if (!logView.verbose.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;
					                                  case CliLogLevel.Debug:
						                                  if (!logView.debug.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;
					                                  case CliLogLevel.Info:
						                                  if (!logView.info.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;
					                                  case CliLogLevel.Warning:
						                                  if (!logView.warning.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;

					                                  case CliLogLevel.Error:
						                                  if (!logView.error.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;

					                                  case CliLogLevel.Fatal:
						                                  if (!logView.fatal.enabled)
						                                  {
							                                  window.AddDelayedAction(() =>
							                                  {
								                                  logView.view.Remove(log);
							                                  });
							                                  return false; // skip
						                                  }

						                                  break;
				                                  }

				                                  if (!string.IsNullOrEmpty(logView.searchText) &&
				                                      !log.message.ToLowerInvariant()
				                                          .Contains(logView.searchText.ToLowerInvariant()))
				                                  {
					                                  logView.view.Remove(log);
					                                  return false;
				                                  }


				                                  // show selected index
				                                  if (index == logView.selectedIndex)
				                                  {
					                                  EditorGUI.DrawRect(position, GUI.skin.settings.selectionColor);
				                                  }

				                                  if (dataList.showLogLevels)
				                                  {
					                                  var iconWidth = 32;
					                                  var iconPadding = 4;
					                                  var iconRect =
						                                  new Rect(position.x + iconPadding, position.y + iconPadding,
						                                           iconWidth - iconPadding * 2,
						                                           position.height - iconPadding * 2);
					                                  var labelRect = new Rect(
						                                  position.x + iconWidth, position.y,
						                                  position.width - iconWidth,
						                                  position.height);
					                                  var logLevel = log.GetLogLevel();
					                                  var hasTexture =
						                                  TryGetTextureForLogLevel(logLevel, out var texture);

					                                  var date = DateTimeOffset.FromUnixTimeMilliseconds(log.timestamp)
					                                                           .ToLocalTime();
					                                  var logMessage = $"[{date:hh:mm:ss}] {log.message}";

					                                  EditorGUI.LabelField(labelRect, logMessage, labelStyle);

					                                  if (hasTexture)
					                                  {
						                                  GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
					                                  }
				                                  }
				                                  else
				                                  {
					                                  var logMessage = $"{log.message}";
					                                  var labelRect =
						                                  new Rect(position.x, position.y, position.width,
						                                           position.height);
					                                  EditorGUI.LabelField(labelRect, logMessage, labelStyle);
				                                  }

				                                  var wasPressed =
					                                  GUI.Button(position, GUIContent.none, EditorStyles.label);

				                                  if (wasPressed)
				                                  {
					                                  var currIndex = index;
					                                  window.AddDelayedAction(() =>
					                                  {
						                                  logView.selectedIndex = currIndex;
						                                  GUIUtility.hotControl = 0;
						                                  GUIUtility.keyboardControl = 0;
					                                  });
				                                  }

				                                  return true;
			                                  });


			// if the user tries to interact with the scroll, then un-tail
			if (Math.Abs(startScrollPosition - logView.logScroll.y) > .0001f)
			{
				logView.isTailing = false;
			}

			// if the user is close enough to the end, then become tail
			if (Math.Abs(maxScroll - logView.logScroll.y) < .01f)
			{
				logView.isTailing = true;
			}

			EditorGUILayout.EndVertical();
		}

		public static void DrawSearchBar(this IDelayedActionWindow window,
		                                 SearchData searchData,
		                                 bool isVerticallyCentered = false,
		                                 string textFieldName=null)
		{
			var searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);

			if (isVerticallyCentered)
			{
				GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
			}

			// note, in Unity 6.2, the height of `EditorStyles.textField` increased, so we need to manually specify line height here. 
			var searchRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField, GUILayout.ExpandWidth(true), GUILayout.MinWidth(30), GUILayout.Height(EditorGUIUtility.singleLineHeight));

			if (isVerticallyCentered)
			{
				GUILayout.FlexibleSpace();
				GUILayout.EndVertical();
			}

			// if there isn't enough space, don't bother rendering it.
			if (searchRect.width > 30)
			{
				EditorGUI.BeginChangeCheck();
				var searchClearRect = new Rect(searchRect.xMax - searchRect.height - 2, searchRect.y,
				                               searchRect.height, searchRect.height);

				EditorGUIUtility.AddCursorRect(searchClearRect, MouseCursor.Link);
				var isButtonHover = searchClearRect.Contains(Event.current.mousePosition);
				var clearButtonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;
				if (searchData != null)
				{
					if (textFieldName != null)
						GUI.SetNextControlName(textFieldName);
					searchData.searchText = EditorGUI.TextField(searchRect, searchData.searchText, searchStyle);
					if (EditorGUI.EndChangeCheck())
					{
						searchData.onEndCheck?.Invoke();
					}

					if (!string.IsNullOrEmpty(searchData.searchText))
					{
						GUI.Button(searchClearRect, GUIContent.none, "SearchCancelButton");

						if (clearButtonClicked)
						{
							window.AddDelayedAction(() =>
							{
								searchData.searchText = null;
								GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								searchData.onEndCheck?.Invoke();
								window.Repaint();
							});
						}
					}
				}
			}
			else
			{
				var noSearchBarSpaceButton = BeamGUI.HeaderButton(null, BeamGUI.iconMenuOptions,
				                                                  padding: 4,
				                                                  iconPadding: -5,
				                                                  drawBorder: true,
				                                                  forcedRect: searchRect);

				if (noSearchBarSpaceButton)
				{
					Vector2 windowSize = new Vector2(Screen.width, 70);
					PopupWindow.Show(
						searchRect,
						new GenericMessagePopup("Hidden Contents",
						                        "There is no available space to display the search bar, please resize the window.",
						                        windowSize));
				}
			}
		}

		static void DrawLogLevelToggle(CliLogLevel logLevel, LogLevelView view, IDelayedActionWindow window, LogView serverLogs)
		{
			
			TryGetTextureForLogLevel(logLevel, out var verboseTexture);

			var count = view.lastCount == 0 ? view.count : view.lastCount;
			var countStr = count > 999 ? "999+" : count.ToString();
			var nextEnabled = GUILayout.Toggle(view.enabled, countStr, 
			                                        new GUIStyle(EditorStyles.toolbarButton)
			                                        {
				                                        padding = new RectOffset(24, 2, 0, 0),
				                                        alignment = TextAnchor.MiddleLeft
			                                        },  GUILayout.MinWidth(35), GUILayout.ExpandWidth(false));
			var lastRect = GUILayoutUtility.GetLastRect();
			var iconRect = new Rect(lastRect.x + 4, lastRect.y + 3, 16, lastRect.height - 6);
			GUI.DrawTexture(iconRect, verboseTexture, ScaleMode.ScaleToFit);
			if (nextEnabled != view.enabled)
			{
				window.AddDelayedAction(() =>
				{
					view.enabled = nextEnabled;
					serverLogs.RebuildView();
				});
			}

		}
		
		static bool TryGetTextureForLogLevel(CliLogLevel logLevel, out Texture texture)
		{
			texture = null;
			switch (logLevel)
			{
				case CliLogLevel.Fatal:
					texture = EditorGUIUtility.FindTexture("CollabConflict Icon");
					break;
				case CliLogLevel.Error:
					texture = EditorGUIUtility.FindTexture("console.erroricon");
					break;
				case CliLogLevel.Warning:
					texture = EditorGUIUtility.FindTexture("console.warnicon");
					break;
				case CliLogLevel.Info:
					texture = EditorGUIUtility.FindTexture("console.infoicon");
					break;
				case CliLogLevel.Debug:
					texture = EditorGUIUtility.FindTexture("d_DebuggerDisabled@2x");
					break;
				case CliLogLevel.Verbose:
					texture = EditorGUIUtility.FindTexture("GameManager Icon");
					break;
			}

			return texture != null;
		}
	}
}
