using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_2018
using UnityEngine.Experimental.Input;
#else
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

#endif

namespace Beamable.InputManagerIntegration
{
	public static class BeamableInput
	{
		public static readonly Dictionary<KeyCode, Key> MatchingEnums = new Dictionary<KeyCode, Key>
			{
				{KeyCode.None, Key.None},
				{KeyCode.Backspace, Key.Backspace},
				{KeyCode.Tab, Key.Tab},
				{KeyCode.Pause, Key.Pause},
				{KeyCode.Escape, Key.Escape},
				{KeyCode.Space, Key.Space},
				{KeyCode.Quote, Key.Quote},
				{KeyCode.Comma, Key.Comma},
				{KeyCode.Minus, Key.Minus},
				{KeyCode.Period, Key.Period},
				{KeyCode.Slash, Key.Slash},
				{KeyCode.Semicolon, Key.Semicolon},
				{KeyCode.Equals, Key.Equals},
				{KeyCode.LeftBracket, Key.LeftBracket},
				{KeyCode.Backslash, Key.Backslash},
				{KeyCode.RightBracket, Key.RightBracket},
				{KeyCode.BackQuote, Key.Backquote},
				{KeyCode.A, Key.A},
				{KeyCode.B, Key.B},
				{KeyCode.C, Key.C},
				{KeyCode.D, Key.D},
				{KeyCode.E, Key.E},
				{KeyCode.F, Key.F},
				{KeyCode.G, Key.G},
				{KeyCode.H, Key.H},
				{KeyCode.I, Key.I},
				{KeyCode.J, Key.J},
				{KeyCode.K, Key.K},
				{KeyCode.L, Key.L},
				{KeyCode.M, Key.M},
				{KeyCode.N, Key.N},
				{KeyCode.O, Key.O},
				{KeyCode.P, Key.P},
				{KeyCode.Q, Key.Q},
				{KeyCode.R, Key.R},
				{KeyCode.S, Key.S},
				{KeyCode.T, Key.T},
				{KeyCode.U, Key.U},
				{KeyCode.V, Key.V},
				{KeyCode.W, Key.W},
				{KeyCode.X, Key.X},
				{KeyCode.Y, Key.Y},
				{KeyCode.Z, Key.Z},
				{KeyCode.Delete, Key.Delete},
				{KeyCode.UpArrow, Key.UpArrow},
				{KeyCode.DownArrow, Key.DownArrow},
				{KeyCode.RightArrow, Key.RightArrow},
				{KeyCode.LeftArrow, Key.LeftArrow},
				{KeyCode.Insert, Key.Insert},
				{KeyCode.Home, Key.Home},
				{KeyCode.End, Key.End},
				{KeyCode.PageUp, Key.PageUp},
				{KeyCode.PageDown, Key.PageDown},
				{KeyCode.F1, Key.F1},
				{KeyCode.F2, Key.F2},
				{KeyCode.F3, Key.F3},
				{KeyCode.F4, Key.F4},
				{KeyCode.F5, Key.F5},
				{KeyCode.F6, Key.F6},
				{KeyCode.F7, Key.F7},
				{KeyCode.F8, Key.F8},
				{KeyCode.F9, Key.F9},
				{KeyCode.F10, Key.F10},
				{KeyCode.F11, Key.F11},
				{KeyCode.F12, Key.F12},
				{KeyCode.Numlock, Key.NumLock},
				{KeyCode.CapsLock, Key.CapsLock},
				{KeyCode.ScrollLock, Key.ScrollLock},
				{KeyCode.RightShift, Key.RightShift},
				{KeyCode.LeftShift, Key.LeftShift},
				{KeyCode.LeftAlt, Key.LeftAlt},
				{KeyCode.LeftWindows, Key.LeftWindows},
				{KeyCode.AltGr, Key.AltGr},
				{KeyCode.RightAlt, Key.AltGr},
				{KeyCode.Keypad0, Key.Numpad0},
				{KeyCode.Keypad1, Key.Numpad1},
				{KeyCode.Keypad2, Key.Numpad2},
				{KeyCode.Keypad3, Key.Numpad3},
				{KeyCode.Keypad4, Key.Numpad4},
				{KeyCode.Keypad5, Key.Numpad5},
				{KeyCode.Keypad6, Key.Numpad6},
				{KeyCode.Keypad7, Key.Numpad7},
				{KeyCode.Keypad8, Key.Numpad8},
				{KeyCode.Keypad9, Key.Numpad9},
				{KeyCode.KeypadEnter, Key.NumpadEnter},
				{KeyCode.LeftControl, Key.LeftCtrl},
				{KeyCode.RightControl, Key.RightCtrl},
			};
		
		public static bool IsActionTriggered(InputActionArg arg)
		{
			return arg?.IsTriggered() ?? false;
		}

		public static void AddInputSystem()
		{
			var eventSystem = new GameObject("EventSystem");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER && !UNITY_2018
			eventSystem.AddComponent<InputSystemUIInputModule>();
#else
			eventSystem.AddComponent<StandaloneInputModule>();
#endif
			if (!eventSystem.TryGetComponent<EventSystem>(out _))
			{
				eventSystem.AddComponent<EventSystem>();
			}
		}

		public static bool IsEscapeKeyDown()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			return Keyboard.current.escapeKey.wasPressedThisFrame;
#else
			return Input.GetKeyDown(KeyCode.Escape);
#endif
		}

		[MenuItem("BEamable/Convert keys")]
		public static void ConvertEnums()
		{
			var oldKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
			var newKeyCodes = (Key[])Enum.GetValues(typeof(Key));
			
			foreach (var oldKey in oldKeyCodes)
			{
				if (MatchingEnums.ContainsKey(oldKey))
					continue;
				bool matchFound = false;
				foreach (var key in newKeyCodes)
				{
					if (key.ToString().ToLowerInvariant().Equals(oldKey.ToString().ToLowerInvariant()))
					{
						MatchingEnums.Add(oldKey, key);
						matchFound = true;
						break;
					}
				}
				if (!matchFound)
					Debug.Log($"Match not found for key {oldKey}");
			}

			foreach (var key in newKeyCodes)
			{
				if (MatchingEnums.ContainsValue(key))
					continue;
				Debug.Log($"(NEW)Match not found for Key.{key}");
			}

			StringBuilder builder = new StringBuilder();
			foreach (KeyValuePair<KeyCode, Key> matchingEnum in MatchingEnums)
			{
				builder.Append($"{{ KeyCode.{matchingEnum.Key}, Key.{matchingEnum.Value} }},");
			}

			Debug.Log(builder.ToString());
		}
	}
}
