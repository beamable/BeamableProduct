using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public class BeamCliTextPopup : PopupWindowContent
	{
		private readonly string _text;
		private readonly string _title;
		private Vector2 _scrollPosition;

		public BeamCliTextPopup(string title, string text)
		{
			_text = text;
			_title = title;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(700, 350);
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
