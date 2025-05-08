using Beamable.Editor.Util;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.OptionDialogWindow
{
	public class OptionDialogWindow : EditorWindow
	{
		private string _message;
		private ButtonInfo[] _buttons;
		
		Vector2 _textScrollPosition;
		Vector2 _buttonsScrollPosition;

		private Action<bool> _onClose;
		
		public static bool ShowModal(string title,
		                                   string message,
		                                   params ButtonInfo[] buttons)
		{
			bool modalResult = false;
			
			var window = CreateInstance<OptionDialogWindow>();
			window._message = message;
			window._buttons = buttons;
			window._onClose = onCloseResult => modalResult = onCloseResult;
			window.titleContent = new GUIContent(title);
			window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 600, 400);
			window.ShowModalUtility();
			return modalResult;
		}

		private void OnGUI()
		{
			DrawMessage();
			DrawButtons();
		}

		private void DrawMessage()
		{
			_textScrollPosition = EditorGUILayout.BeginScrollView(_textScrollPosition);
			var textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
			{
				richText = true,
				fontSize = 12,
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(10, 10, 10, 10),
			};
			EditorGUILayout.LabelField(_message, textStyle);
			EditorGUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
		}
		
		private void DrawButtons()
		{
			_buttonsScrollPosition = EditorGUILayout.BeginScrollView(_buttonsScrollPosition, false,false, GUILayout.Height(50));
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			foreach (var btnInfo in _buttons)
			{
				float buttonSize = GUI.skin.button.CalcSize(new GUIContent(btnInfo.Name)).x + 30f;

				GUIStyle buttonStyle = BeamGUI.ColorizeButton(btnInfo.Color, 0.3f);
				
				if (GUILayout.Button(btnInfo.Name, buttonStyle,GUILayout.Width(buttonSize), GUILayout.Height(40)))
				{
					bool res = btnInfo.OnClick == null || btnInfo.OnClick.Invoke();
					_onClose(res);
					Close();
				}
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndScrollView();
			GUILayout.Space(10);
			
		}

		public class ButtonInfo
		{
			public string Name;
			public Func<bool> OnClick;
			public Color Color = Color.white;
		}
	}
}
