using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		private static GUIStyle placeholderStyle;
		
		
		public static string PlaceholderTextField(Rect rect, string text, string placeholder, GUIStyle styles)
		{
			var nextText = EditorGUI.TextField(rect, text, styles);
			if (string.IsNullOrEmpty(text))
			{
				if (placeholderStyle == null)
				{
					placeholderStyle = new GUIStyle(EditorStyles.label)
					{
						padding = new RectOffset(4, 0, 0, 0),
						normal = new GUIStyleState
						{
							textColor = Color.Lerp(EditorStyles.label.normal.textColor, new Color(1, 1, 1, 0f), .5f)
						}
					};
				}
				
				EditorGUI.LabelField(rect, placeholder, placeholderStyle);
			}
			return nextText;
		}
		
		public static string LayoutPlaceholderTextField(string text, string placeholder, GUIStyle styles, params GUILayoutOption[] options)
		{
			var rect = GUILayoutUtility.GetRect(new GUIContent(text), styles, options);
			return PlaceholderTextField(rect, text, placeholder, styles);
		}
	}
}