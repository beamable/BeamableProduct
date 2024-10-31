using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		public static void LoadingSpinnerWithState(string status)
		{
			const int padding = 12;
			
			var content = new GUIContent(status);
			var style = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter
			};

			var size = style.CalcSize(content);
			
			
			var bounds = GUILayoutUtility.GetRect(content, style, GUILayout.MinHeight(32 + padding));

			var labelRect = new Rect(bounds.x, bounds.yMax - size.y, bounds.width, size.y);
			var spinnerRect = new Rect(bounds.x, bounds.y, bounds.width, bounds.height - size.y - padding);

			var spinnerIndex = (int)( (Time.realtimeSinceStartup*12f) % BeamGUI.unitySpinnerTextures.Length);
			GUI.DrawTexture(spinnerRect, BeamGUI.unitySpinnerTextures[spinnerIndex], ScaleMode.ScaleToFit);
			
			EditorGUI.LabelField(labelRect, content, style);

		}
	}
}
