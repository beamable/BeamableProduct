#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_2018
using UnityEngine.Experimental.Input;
#else
using UnityEngine.InputSystem;
#endif
#endif

using Beamable.Common;
using Beamable.ConsoleCommands;
using Beamable.InputManagerIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Beamable.Console
{
	public class BeamableAdminConsole : MonoBehaviour
	{
		public static BeamableAdminConsole Instance { get; private set; }
		public event Action<string> OnLogLine;
		public bool IsActive => _isActive;
		public bool IsInitialized => _isInitialized;
		

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
		/// <summary>
		/// Optional <see cref="InputAction"/> used to toggle the console open/closed
		/// with the new Input System.  Falls back to <see cref="ConsoleConfiguration"/>'s
		/// toggle action and then to the Backtick/Grave key if left null.
		/// Call <c>Enable()</c> on the action before assigning it, or set
		/// <see cref="AutoEnableToggleAction"/> to true (default).
		/// </summary>
		public InputAction CustomToggleAction;

		/// <summary>
		/// When true, <see cref="StartListening"/> automatically calls
		/// <c>CustomToggleAction.Enable()</c> if <see cref="CustomToggleAction"/> is set.
		/// Default: true.
		/// </summary>
		public bool AutoEnableToggleAction = true;
#endif
		

		#region Private State
		
		private bool   _isInitialized;
		private bool   _isActive;
		private bool   _focusInputNextFrame;

		private string  _inputText   = string.Empty;
		private string  _consoleText = string.Empty;
		private Vector2 _scrollPosition;

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
		private bool     _stylesReady;
		private readonly Color _backgroundColor = new Color(0f, 0f, 0f, 0.85f);
		private readonly float _heightRatio = 0.4f;
		
		private const string InputControlName = "SAConsoleInput";

		#endregion

		#region MonoBehaviour Lifecycle

		private void Awake()
		{
			_history       = new ConsoleHistory();
			_autoCompleter = new AutoCompleter(_commands);
		}

		private void Start()
		{
			/*
			if (!AutoStart) return;
			StartListening();
			Initialize();*/
		}

		#endregion

		#region Initialization
		
		public void InitializeConsole(BeamContext beamContext)
		{
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
			
			// Start Listening Inputs
			StartListening();
		}

		public void DestroyConsole()
		{
			Destroy(this);
		}
		
		/*
		/// <summary>
		/// Fire-and-forget async initialization: connects to Beamable and loads all
		/// console commands.  Check <see cref="IsInitialized"/> or subscribe to
		/// <see cref="OnLogLine"/> for the "Console ready" notification.
		/// </summary>
		public async void Initialize()
		{
			try
			{
				await InitializeAsync();
			}
			catch (Exception e)
			{
				Debug.LogError($"[StandAloneConsoleFlow] Initialization failed: {e}");
			}
		}*/
		
		
		
		/*
		/// <summary>
		/// Awaitable version of <see cref="Initialize"/>; returns a Beamable
		/// <see cref="Promise"/> that completes when the console is ready.
		/// </summary>
		public async Promise InitializeAsync()
		{
			_isInitialized = false;

			var ctx = BeamContext.ForPlayer(PlayerCode);
			await ctx.OnReady;

			DetachConsole();
			_beamableConsole = ctx.ServiceProvider.GetService<BeamableConsole>();
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
		}*/
		
		/// <summary>
		/// Marks this GameObject as <c>DontDestroyOnLoad</c> and registers the global
		/// singleton.  Call once after adding the component (or from <c>Start</c>).
		/// </summary>
		public void StartListening()
		{
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			Instance = this;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if (AutoEnableToggleAction && CustomToggleAction != null && !CustomToggleAction.enabled)
				CustomToggleAction.Enable();
#endif
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

		/// <summary>Toggles between shown and hidden.</summary>
		public void ToggleConsole()
		{
			ShowConsole();
			if (_isActive) HideConsole();
			else           ShowConsole();
		}

		#endregion

		#region Logging

		/// <summary>Appends a line to the console output and fires <see cref="OnLogLine"/>.</summary>
		public void Log(string line)
		{
			if (line == null) return;
			Debug.Log(line);
			_consoleText      += Environment.NewLine + line;
			_scrollPosition.y  = float.MaxValue;
			TrimConsoleText();
			OnLogLine?.Invoke(line);
		}

		#endregion

		#region Update & OnGUI

		private void OnGUI()
		{
			if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.BackQuote)
			{
				if (!_isActive)
				{
					ShowConsole();
					Event.current.Use();
					return;
				}
			}
			
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

					case KeyCode.Escape:
						HideConsole();
						Event.current.Use();
						break;
				}
			}

			// Guard: Hide() might have been called by Escape above
			if (!_isActive) return;

			var windowRect = new Rect(0f, 0f, Screen.width, Screen.height * _heightRatio);
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
			// Title bar
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("<b>▶ BEAMABLE ADMIN CONSOLE</b>", _titleStyle);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("✕", GUILayout.Width(28), GUILayout.Height(22)))
				{
					HideConsole();
					GUILayout.EndHorizontal();
					return;
				}
			}
			GUILayout.EndHorizontal();

			// Thin separator
			GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));

			// Scrollable output area — 54 px reserved for title + input row
			float outputHeight = Mathf.Max(20f, Screen.height * _heightRatio - 54f);
			_scrollPosition = GUILayout.BeginScrollView(
				_scrollPosition, GUILayout.Height(outputHeight), GUILayout.ExpandWidth(true));
			{
				GUILayout.Label(_consoleText, _outputStyle);
			}
			GUILayout.EndScrollView();

			// Input row:  ">"  [text field]  [suggestion hint]
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(">", _promptStyle, GUILayout.Width(18));

				GUI.SetNextControlName(InputControlName);
				var prevBg   = GUI.backgroundColor;
				GUI.backgroundColor = GUI.GetNameOfFocusedControl() == InputControlName
					? new Color(0.24f, 0.24f, 0.24f, 1f)
					: new Color(0.18f, 0.18f, 0.18f, 1f);
				var newInput = GUILayout.TextField(_inputText, _inputStyle);
				GUI.backgroundColor = prevBg;
				if (newInput != _inputText)
				{
					_inputText = newInput;
					_autoCompleter.Update(_inputText);
				}

				var suggestion = _autoCompleter.CurrentSuggestion;
				if (!string.IsNullOrEmpty(suggestion))
				{
					GUILayout.Label($"↹  {suggestion}", _suggestionStyle, GUILayout.ExpandWidth(false));
				}
			}
			GUILayout.EndHorizontal();
		}
		
		private void MakeStyle()
		{
			if (_stylesReady) return;
			_stylesReady = true;

			_titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize  = 13,
				fontStyle = FontStyle.Bold,
				richText  = true,
				normal    = { textColor = Color.white },
				padding   = new RectOffset(6, 4, 4, 2)
			};

			_outputStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize     = 12,
				wordWrap     = true,
				richText     = true,
				stretchWidth = true,
				normal       = { textColor = new Color(0.87f, 0.87f, 0.87f) },
				padding      = new RectOffset(6, 6, 2, 2)
			};

			_inputStyle = new GUIStyle(GUI.skin.textField)
			{
				fontSize = 13,
				normal   = { textColor = Color.white },
				hover    = { textColor = Color.white },
				focused  = { textColor = Color.white },
				active   = { textColor = Color.white }
			};

			_promptStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize  = 14,
				fontStyle = FontStyle.Bold,
				normal    = { textColor = new Color(0.4f, 0.9f, 0.4f) },
				padding   = new RectOffset(4, 0, 3, 0)
			};

			_suggestionStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize     = 12,
				fontStyle    = FontStyle.Italic,
				stretchWidth = false,
				normal       = { textColor = new Color(0.55f, 0.78f, 1f) },
				padding      = new RectOffset(6, 4, 3, 0)
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

		private bool ShouldShow()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// 1. Custom user-supplied InputAction (new Input System)
			if (CustomToggleAction != null && CustomToggleAction.triggered) return true;
#endif
			// 2. Shared ConsoleConfiguration toggle action (works for both input backends)
			if (BeamableInput.IsActionTriggered(ConsoleConfiguration.Instance.ToggleAction)) return true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// 3. New Input System keyboard fallback — Backtick / Grave
			//    (used when ConsoleConfiguration.ToggleAction has no asset assigned)
			return Keyboard.current?.backquoteKey.wasPressedThisFrame ?? false;
#else
			return false; // legacy path is fully covered by ConsoleConfiguration above
#endif
		}

		private bool IsEnabled()
		{
			
#if UNITY_EDITOR
			return true;
#else
			return ConsoleConfiguration.Instance.ForceEnabled
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
