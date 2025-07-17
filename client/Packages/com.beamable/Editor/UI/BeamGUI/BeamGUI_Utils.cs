using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		
		public static readonly float StandardVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		
		public static void DrawVerticalSeparatorLine(RectOffset margin = null, Color? color = null)
		{
			DrawSeparatorLine(true, margin, color);
		}
		
		public static void DrawHorizontalSeparatorLine(RectOffset margin = null, Color? color = null)
		{
			DrawSeparatorLine(false, margin, color);
		}
		
		public static void DrawSeparatorLine(bool isVertical, RectOffset margin = null, Color? color = null)
		{
			var lineStyle = new GUIStyle
			{
				normal =
				{
					background = color.HasValue
						? CreateColorTexture(color.Value)
						: EditorGUIUtility.whiteTexture
				},
				margin = margin ?? new RectOffset(0, 0, 0, 0)
			};
			if (isVertical)
			{
				lineStyle.fixedWidth = 1;
				GUILayout.Box(GUIContent.none, lineStyle,
				              GUILayout.ExpandHeight(true),
				              GUILayout.Width(1f));
			}
			else
			{
				lineStyle.fixedHeight = 1;
				GUILayout.Box(GUIContent.none, lineStyle,
				              GUILayout.ExpandWidth(true),
				              GUILayout.Height(1f));
			}
		}

		public static void DrawVerticalSeparatorLine(float x, float y, float height, Color color)
		{
			Rect lineRect = new Rect(x, y, 1f, height);
			EditorGUI.DrawRect(lineRect, color);
		}

		

		public static void DrawHorizontalSeparatorLine(float x, float y, float width, Color color)
		{
			Rect lineRect = new Rect(x, y, width, 1f);
			EditorGUI.DrawRect(lineRect, color);
		}

		

		public static Texture2D CreateColorTexture(Color color)
		{
			var texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return texture;
		}
	}
}
