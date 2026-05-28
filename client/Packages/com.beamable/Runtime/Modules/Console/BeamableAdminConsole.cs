using Beamable.ConsoleCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Beamable.Console
{
	public class BeamableAdminConsole : MonoBehaviour
	{
		public static BeamableAdminConsole Instance { get; private set; }
		public event Action<string> OnLogLine;
		public bool IsActive => _isActive;
		public bool IsInitialized => _isInitialized;

		#region Private State

		private bool   _isInitialized;
		private bool   _isActive;
		private bool   _focusInputNextFrame;
		private bool   _moveCursorToEndNextFrame;

		private string  _inputText   = string.Empty;
		private string  _consoleText = string.Empty;
		private Vector2 _scrollPosition;

		// 3-finger touch detection for Android
		private int _fingerCount;
		private bool _waitForRelease;
		private Vector2 _averagePositionStart;

		// Virtual keyboard state tracking
		private bool _wasVirtualKeyboardOpen;

		// Drag-to-scroll state
		private bool  _isDraggingScroll;
		private float _dragLastMouseY;
		private Rect  _scrollViewRect;

		private readonly Dictionary<string, ConsoleCommandEntry> _commands =
			new Dictionary<string, ConsoleCommandEntry>(StringComparer.OrdinalIgnoreCase);

		private ConsoleHistory  _history;
		private AutoCompleter   _autoCompleter;
		private BeamableConsole _beamableConsole;
		private BeamContext _beamContext;

		// IMGUI styles — built lazily inside OnGUI so GUI.skin is available
		private GUIStyle _titleStyle;
		private GUIStyle _outputStyle;
		private GUIStyle _inputStyle;
		private GUIStyle _promptStyle;
		private GUIStyle _suggestionStyle;
		private readonly Color _backgroundColor = new Color(0f, 0f, 0f, 0.85f);

		private const string InputControlName = "SAConsoleInput";

		private const float ReferenceResolutionHeight = 1080f;
		private float _screenScale = 1f;
		private float _lastScreenScale = -1f;

		#endregion

		#region MonoBehaviour Lifecycle

		private void Awake()
		{
			_history       = new ConsoleHistory();
			_autoCompleter = new AutoCompleter(_commands);
		}

		private void Update()
		{
			if (!_isInitialized) return;
			
			if (!IsEnabled()) return;
			
			if (CheckToggleKey())
			{
				if (!_isActive){ShowConsole();}
				else{HideConsole();}
			}

			if (ShouldToggleByTouch())
			{
				if (!_isActive){ShowConsole();}
				else{HideConsole();}
			}
		}

		private bool CheckToggleKey()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if (Keyboard.current.backquoteKey.wasPressedThisFrame)
			{
				return true;
			}
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
			if (Input.GetKeyDown(KeyCode.BackQuote))
			{
				return true;
			}
#endif
			return false;
		}

		#endregion

		#region Initialization
		
		public void InitializeConsole(BeamContext beamContext)
		{
			Instance = this;
			
			DetachConsole();
			_beamContext = beamContext;
			_beamableConsole = _beamContext.ServiceProvider.GetService<BeamableConsole>();
			if(_beamableConsole == null)
			{
				Debug.LogError("ConsoleFlow Unable to retrieve BeamableConsole from context.");
				return;
			}
			
			AttachConsole();
			
			try
			{
				_beamableConsole.LoadCommands();
			}
			catch (Exception)
			{
				Debug.LogError("[StandAloneConsoleFlow] Unable to load console commands.");
			}

			_isInitialized = true;
			Log("Console ready");
			
		}
		
		#endregion

		#region Visibility

		/// <summary>Opens the console window and focuses the input field.</summary>
		public void ShowConsole()
		{
			_isActive            = true;
			_inputText           = string.Empty;
			_focusInputNextFrame = true;
			_scrollPosition.y    = float.MaxValue; // jump to bottom
		}

		/// <summary>Hides the console window.</summary>
		public void HideConsole()
		{
			_isActive  = false;
			_inputText = string.Empty;
			_autoCompleter.Reset();
		}

		#endregion

		#region Logging

		/// <summary>Appends a line to the console output and fires <see cref="OnLogLine"/>.</summary>
		public void Log(string line)
		{
			if (line == null) return;
			_consoleText      += Environment.NewLine + line;
			_scrollPosition.y  = float.MaxValue;
			TrimConsoleText();
			OnLogLine?.Invoke(line);
		}

		#endregion

		#region Update & OnGUI

		private void OnGUI()
		{
			_screenScale = Screen.height / ReferenceResolutionHeight * ConsoleConfiguration.Instance.UISize;
			
			if (!_isActive) return;

			MakeStyle();

			// Keyboard shortcuts processed BEFORE control drawing so we can consume
			// events (e.g. Tab, UpArrow) before the TextField sees them.
			if (Event.current.type == EventType.KeyDown)
			{
				switch (Event.current.keyCode)
				{
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
						if (!string.IsNullOrEmpty(_inputText))
							SubmitInput(_inputText);
						Event.current.Use();
						break;

					case KeyCode.Tab:
						_autoCompleter.Accept(ref _inputText);
						_moveCursorToEndNextFrame = true;
						_focusInputNextFrame = true;
						Event.current.Use();
						break;

					case KeyCode.UpArrow:
						_inputText = _history.Previous();
						Event.current.Use();
						break;

					case KeyCode.DownArrow:
						_inputText = _history.Next();
						Event.current.Use();
						break;
					
					// Forced hide here if using the Old Input System since the tab intercept all input calls
#if ENABLE_LEGACY_INPUT_MANAGER
					case KeyCode.BackQuote:
						HideConsole();
						break;
#endif
				}
			}

			// Guard: Hide() might have been called by Escape above
			if (!_isActive) return;

			var windowRect = new Rect(0f, 0f, Screen.width, Screen.height * ConsoleConfiguration.Instance.Height);
			var prevColor  = GUI.color;
			GUI.color = _backgroundColor;
			GUI.DrawTexture(windowRect, Texture2D.whiteTexture);
			GUI.color = prevColor;

			GUILayout.BeginArea(windowRect);
			DrawConsoleContent();
			GUILayout.EndArea();

			// Deferred focus — must happen in a Layout event to avoid IMGUI control-ID mismatches.
			if (_focusInputNextFrame && Event.current.type == EventType.Layout)
			{
				GUI.FocusControl(InputControlName);
				_focusInputNextFrame = false;
			}
		}
		
		private void DrawConsoleContent()
		{
			var scaledTitleHeight = Mathf.RoundToInt(56 * _screenScale);
			var scaledPromptWidth = Mathf.RoundToInt(36 * _screenScale);
			var scaledSeparatorHeight = Mathf.Max(1, Mathf.RoundToInt(2 * _screenScale));
			var scaledInputHeight = Mathf.RoundToInt(56 * _screenScale);
			var scaledInputMinHeight = Mathf.RoundToInt(44 * _screenScale);

			// Title bar
			GUILayout.BeginHorizontal(GUILayout.Height(scaledTitleHeight));
			{
				GUILayout.Label("<b>▶ BEAMABLE ADMIN CONSOLE</b>", _titleStyle);
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();

			// Thin separator
			GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(scaledSeparatorHeight));

			// Scrollable output area — reserved space for title + input row
			float reservedHeight = scaledTitleHeight + scaledInputHeight + Mathf.RoundToInt(2 * _screenScale);
			float outputHeight = Mathf.Max(Mathf.RoundToInt(40 * _screenScale), Screen.height * ConsoleConfiguration.Instance.Height - reservedHeight);
			_scrollPosition = GUILayout.BeginScrollView(
				_scrollPosition, GUILayout.Height(outputHeight), GUILayout.ExpandWidth(true));
			{
				GUILayout.Label(_consoleText, _outputStyle);
			}
			GUILayout.EndScrollView();

			if (Event.current.type == EventType.Repaint)
				_scrollViewRect = GUILayoutUtility.GetLastRect();

			HandleScrollViewDrag();

			// Input row:  ">"  [text field]  [suggestion hint]
			GUILayout.BeginHorizontal(GUILayout.Height(scaledInputHeight));
			{
				if (GUILayout.Button("Run", _promptStyle, GUILayout.Width(scaledPromptWidth * 2f), GUILayout.Height(scaledInputMinHeight)) && !string.IsNullOrEmpty(_inputText))
					SubmitInput(_inputText);

				GUI.SetNextControlName(InputControlName);
				var prevBg   = GUI.backgroundColor;
				GUI.backgroundColor = GUI.GetNameOfFocusedControl() == InputControlName
					? new Color(0.24f, 0.24f, 0.24f, 1f)
					: new Color(0.18f, 0.18f, 0.18f, 1f);
				var newInput = GUILayout.TextField(_inputText, _inputStyle, GUILayout.MinHeight(scaledInputMinHeight));
				GUI.backgroundColor = prevBg;
				if (newInput != _inputText)
				{
					_inputText = newInput;
					_autoCompleter.Update(_inputText);
				}

				if (_moveCursorToEndNextFrame && Event.current.type == EventType.Repaint)
				{
					var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
					if (editor != null)
					{
						editor.text = _inputText;
						editor.cursorIndex = _inputText.Length;
						editor.selectIndex = _inputText.Length;
						editor.MoveTextEnd();
					}
					_moveCursorToEndNextFrame = false;
				}

				var suggestion = _autoCompleter.CurrentSuggestion;
				if (!string.IsNullOrEmpty(suggestion))
				{
					if (GUILayout.Button($"↹  {suggestion}", _suggestionStyle, GUILayout.ExpandWidth(false)))
					{
						_autoCompleter.Accept(ref _inputText);
						_moveCursorToEndNextFrame = true;
						_focusInputNextFrame      = true;
					}
				}
			}
			GUILayout.EndHorizontal();
		}
		
		private void HandleScrollViewDrag()
		{
			var e = Event.current;
			switch (e.type)
			{
				case EventType.MouseDown when _scrollViewRect.Contains(e.mousePosition):
					_isDraggingScroll = true;
					_dragLastMouseY   = e.mousePosition.y;
					e.Use();
					break;
				case EventType.MouseDrag when _isDraggingScroll:
					_scrollPosition.y += _dragLastMouseY - e.mousePosition.y;
					_dragLastMouseY    = e.mousePosition.y;
					e.Use();
					break;
				case EventType.MouseUp when _isDraggingScroll:
					_isDraggingScroll = false;
					e.Use();
					break;
			}
		}

		private void MakeStyle()
		{
			if (Mathf.Abs(_lastScreenScale - _screenScale) < 0.01f) return;
			_lastScreenScale = _screenScale;

			var scaledFontSize = Mathf.RoundToInt(20 * _screenScale);
			var scaledPaddingH = Mathf.RoundToInt(12 * _screenScale);
			var scaledPaddingV = Mathf.RoundToInt(8 * _screenScale);
			var scaledPaddingSmall = Mathf.RoundToInt(4 * _screenScale);
			var scaledPaddingTiny = Mathf.RoundToInt(6 * _screenScale);

			_titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize  = scaledFontSize,
				fontStyle = FontStyle.Bold,
				richText  = true,
				normal    = { textColor = Color.white },
				padding   = new RectOffset(scaledPaddingH, scaledPaddingH / 2, scaledPaddingV, scaledPaddingSmall)
			};

			_outputStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize     = scaledFontSize,
				wordWrap     = true,
				richText     = true,
				stretchWidth = true,
				normal       = { textColor = new Color(0.87f, 0.87f, 0.87f) },
				padding      = new RectOffset(scaledPaddingH, scaledPaddingH, scaledPaddingSmall, scaledPaddingSmall)
			};

			_inputStyle = new GUIStyle(GUI.skin.textField)
			{
				fontSize   = scaledFontSize,
				alignment  = TextAnchor.MiddleLeft,
				normal     = { textColor = Color.white },
				hover      = { textColor = Color.white },
				focused    = { textColor = Color.white },
				active     = { textColor = Color.white }
			};

			_promptStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize  = scaledFontSize,
				fontStyle = FontStyle.Bold,
				normal    = { textColor = new Color(0.4f, 0.9f, 0.4f) },
				padding   = new RectOffset(Mathf.RoundToInt(8 * _screenScale), 0, scaledPaddingTiny, 0)
			};

			_suggestionStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize     = scaledFontSize,
				fontStyle    = FontStyle.Italic,
				stretchWidth = false,
				normal       = { textColor = new Color(0.55f, 0.78f, 1f) },
				padding      = new RectOffset(scaledPaddingH, Mathf.RoundToInt(8 * _screenScale), scaledPaddingTiny, 0)
			};

		}

		#endregion

		#region Command Execution

		private void SubmitInput(string txt)
		{
			_history.Push(txt);
			_inputText = string.Empty;
			_autoCompleter.Reset();

			var parts = txt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) return;

			var args = new string[parts.Length - 1];
			for (var i = 1; i < parts.Length; i++) args[i - 1] = parts[i];
			
			var result = _beamableConsole.Execute(parts[0], args);
			if (!string.IsNullOrEmpty(result)) Log(result);
		}

		private void AttachConsole()
		{
			if (_beamableConsole == null) return;
			_beamableConsole.OnLog              += Log;
			_beamableConsole.OnExecute          += HandleExecute;
			_beamableConsole.OnCommandRegistered += HandleCommandRegistered;
		}

		private void DetachConsole()
		{
			if (_beamableConsole == null) return;
			_beamableConsole.OnLog              -= Log;
			_beamableConsole.OnExecute          -= HandleExecute;
			_beamableConsole.OnCommandRegistered -= HandleCommandRegistered;
		}

		private void HandleCommandRegistered(BeamableConsoleCommandAttribute attr, ConsoleCommandCallback cb)
		{
			foreach (var name in attr.Names)
				_commands[name.ToLower()] = new ConsoleCommandEntry { Attribute = attr, Callback = cb };

			_autoCompleter.Refresh();
		}

		private string HandleExecute(string command, string[] args)
		{
			if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
				return BuildHelp(args);

			if (_commands.TryGetValue(command.ToLower(), out var entry))
			{
				var echo = "> " + command + (args.Length > 0 ? " " + string.Join(" ", args) : string.Empty);
				Log(echo);
				return entry.Callback(args);
			}

			return "Unknown command: " + command;
		}

		private string BuildHelp(string[] args)
		{
			if (args.Length == 0)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Available commands:");
				foreach (var entry in _commands.Values.Distinct())
					sb.AppendLine($"  {entry.Attribute.Usage}  —  {entry.Attribute.Description}");
				return sb.ToString();
			}

			var key = args[0].ToLower();
			return _commands.TryGetValue(key, out var found)
				? $"{found.Attribute.Usage}\n  {found.Attribute.Description}"
				: $"No help found for '{args[0]}'.";
		}

		#endregion

		#region Toggle Detection

		private bool ShouldToggleByTouch()
		{
			var shouldToggle = false;
			
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			
			if (Touchscreen.current != null)
			{
				var fingerCount = 0;
				var averagePosition = Vector2.zero;
				foreach (var touch in Touchscreen.current.touches)
				{
					var phase = touch.phase.ReadValue();
					if (phase != UnityEngine.InputSystem.TouchPhase.Ended &&
					    phase != UnityEngine.InputSystem.TouchPhase.Canceled &&
					    phase != UnityEngine.InputSystem.TouchPhase.None)
					{
						fingerCount++;
						averagePosition += touch.position.ReadValue();
					}
				}

				switch (fingerCount)
				{
					case 3 when !_waitForRelease:
					{
						averagePosition /= 3;
						if (_fingerCount != 3)
						{
							_averagePositionStart = averagePosition;
						}
						else if ((_averagePositionStart - averagePosition).magnitude > 20.0f)
						{
							_waitForRelease = true;
							shouldToggle = true;
						}
						break;
					}
					case 0 when _waitForRelease:
						_waitForRelease = false;
						break;
				}

				_fingerCount = fingerCount;
			}
			
			return shouldToggle;
#else
			// 3-finger touch detection (legacy input)
			var fingerCount = 0;
			var averagePosition = Vector2.zero;
			var touchCount = Input.touchCount;
			for (var i = 0; i < touchCount; ++i)
			{
				var touch = Input.GetTouch(i);
				if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
				{
					fingerCount++;
					averagePosition += touch.position;
				}
			}

			switch (fingerCount)
			{
				case 3 when !_waitForRelease:
				{
					averagePosition /= 3;
					if (_fingerCount != 3)
					{
						_averagePositionStart = averagePosition;
					}
					else if ((_averagePositionStart - averagePosition).magnitude > 20.0f)
					{
						_waitForRelease = true;
						shouldToggle = true;
					}
					break;
				}
				case 0 when _waitForRelease:
					_waitForRelease = false;
					break;
			}

			_fingerCount = fingerCount;
			return shouldToggle;
#endif
		}

		private bool IsEnabled()
		{
			
#if UNITY_EDITOR
			return true;
#else
			return ConsoleConfiguration.Instance.EnableAdminConsole
				|| _beamContext.AuthorizedUser.Value.HasScope("cli:console");
#endif
		}

		#endregion

		#region Text Trimming

		/// Mirrors ConsoleFlow's vertex-budget-based pruning to keep IMGUI from
		/// hitting Unity's 65K-vertex text mesh limit.
		private void TrimConsoleText()
		{
			const int charsPerVertex = 6;
			const int vertexLimit    = 65 * 1024;

			int excess = (_consoleText.Length * charsPerVertex) - vertexLimit;
			if (excess <= 0) return;

			int cutAt   = excess / charsPerVertex;
			int newLine = _consoleText.IndexOf(Environment.NewLine, cutAt, StringComparison.Ordinal);
			_consoleText = newLine >= 0
				? _consoleText.Substring(newLine + Environment.NewLine.Length)
				: string.Empty;
		}

		#endregion

		#region Nested Console Command Entry

		private struct ConsoleCommandEntry
		{
			public BeamableConsoleCommandAttribute Attribute;
			public ConsoleCommandCallback          Callback;
		}

		#endregion

		#region Nested Console History

		private class ConsoleHistory
		{
			private readonly List<string> _items = new List<string>();
			private int _pos;

			public void Push(string text)
			{
				if (string.IsNullOrEmpty(text)) return;
				_items.Add(text);
				_pos = _items.Count;
			}

			public string Previous()
			{
				if (_items.Count == 0) return string.Empty;
				_pos = Mathf.Max(0, _pos - 1);
				return _items[_pos];
			}

			public string Next()
			{
				_pos++;
				if (_pos < _items.Count) return _items[_pos];
				_pos = _items.Count;
				return string.Empty;
			}
		}

		#endregion

		#region Nested Auto Completer

		private class AutoCompleter
		{
			private readonly Dictionary<string, ConsoleCommandEntry> _source;
			private List<string> _matches    = new List<string>();
			private int          _matchIndex;
			private string       _lastInput  = string.Empty;

			public string CurrentSuggestion { get; private set; } = string.Empty;

			public AutoCompleter(Dictionary<string, ConsoleCommandEntry> source) => _source = source;

			/// <summary>Re-runs the last search (called after new commands are registered).</summary>
			public void Refresh() => Update(_lastInput);

			public void Update(string input)
			{
				_lastInput  = input;
				_matchIndex = 0;

				if (string.IsNullOrWhiteSpace(input))
				{
					_matches          = _source.Keys.OrderBy(k => k).ToList();
					CurrentSuggestion = string.Empty;
				}
				else
				{
					_matches = _source.Keys
						.Where(k => k.StartsWith(input, StringComparison.OrdinalIgnoreCase))
						.OrderBy(k => k)
						.ToList();
					CurrentSuggestion = _matches.Count > 0 ? _matches[0] : string.Empty;
				}
			}

			/// <summary>
			/// Accepts the current suggestion (sets <paramref name="inputText"/>).
			/// Pressing Tab again cycles to the next matching command.
			/// </summary>
			public void Accept(ref string inputText)
			{
				if (_matches.Count == 0) return;

				if (string.Equals(inputText, CurrentSuggestion, StringComparison.OrdinalIgnoreCase))
				{
					_matchIndex       = (_matchIndex + 1) % _matches.Count;
					CurrentSuggestion = _matches[_matchIndex];
				}

				inputText = CurrentSuggestion;
			}

			public void Reset()
			{
				CurrentSuggestion = string.Empty;
				_matches.Clear();
				_matchIndex = 0;
			}
		}

		#endregion
	}

}
