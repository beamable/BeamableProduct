using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{


		public static bool LayoutToggle(bool on, int toggleSize = 24, int yShift = 0)
		{

			var normalOff = EditorGUIUtility.IconContent("ShurikenToggleNormal@2x");
			var hoverOff = EditorGUIUtility.IconContent("ShurikenToggleHover@2x");
			var normalOn = EditorGUIUtility.IconContent("ShurikenToggleNormalOn@2x");
			var hoverOn = EditorGUIUtility.IconContent("ShurikenToggleHoverOn@2x");

			var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Width(toggleSize), GUILayout.Height(toggleSize));
			rect = new Rect(rect.x, rect.y + yShift, rect.width, rect.height);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

			var isButtonHover = rect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			var normal = on ? normalOn : normalOff;
			var hover = on ? hoverOn : hoverOff;
			var texture = isButtonHover ? hover : normal;

			{ // texture
				GUI.DrawTexture(rect, texture.image);
			}

			if (buttonClicked)
			{
				return !on;
			}

			return on;
		}
	}
}
