using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Utility
{
	public class GenericMessagePopup : PopupWindowContent
	{
		private readonly string _text;
		private readonly string _title;
		private readonly Vector2 _windowSize;
		private Vector2 _scrollPosition;

		public GenericMessagePopup(string title, string text, Vector2 windowSize)
		{
			_text = text;
			_title = title;
			_windowSize = windowSize;
		}

		public override Vector2 GetWindowSize()
		{
			return _windowSize;
		}

		public override void OnGUI(Rect rect)
		{
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
			GUILayout.Label(_title, EditorStyles.boldLabel);
			GUILayout.Space(5);
			GUILayout.TextArea(_text);
			GUILayout.EndScrollView();
		}
	}
}
