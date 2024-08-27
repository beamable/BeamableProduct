using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public class BeamCliJsonPopup : PopupWindowContent
	{
		private readonly string _json;
		private Vector2 _scrollPosition;

		public BeamCliJsonPopup(string json)
		{
			_json = json;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(500, 300);
		}

		public override void OnGUI(Rect rect)
		{
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
			var highlighted = JsonHighlighterUtil.HighlightJson(_json);
			BeamCliWindow.DrawJsonBlock(highlighted);
			GUILayout.EndScrollView();
		}
	}
}
