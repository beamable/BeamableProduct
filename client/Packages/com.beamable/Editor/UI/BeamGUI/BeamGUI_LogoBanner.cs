using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		public static void DrawLogoBanner()
		{

			var logo = BeamGUI.iconLogoHeader;

			var logoRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(50),
			                                        GUILayout.ExpandWidth(true));
			var logoAspect = logo.width / (float) logo.height;
			var shadowWidth = logoAspect * logoRect.height;

			var shadowRect = new Rect(logoRect.x + logoRect.width * .5f - shadowWidth * .5f, logoRect.y,
			                          shadowWidth, logoRect.height);

			const int shadowYOffset = 6;
			var leftShadowRect = new Rect(shadowRect.x - 12, shadowRect.y + shadowYOffset, 20, shadowRect.height);
			var rightShadowRect =
				new Rect(shadowRect.xMax - 20, shadowRect.y + shadowYOffset, 12, shadowRect.height);
			var centerShadowRect = new Rect(leftShadowRect.xMax, shadowRect.y + shadowYOffset,
			                                (rightShadowRect.xMin - leftShadowRect.xMax), shadowRect.height);

			// draw the shadow as 3 parts to avoid texture stretching
			GUI.DrawTextureWithTexCoords(leftShadowRect, BeamGUI.iconShadowSoftA, new Rect(0, 0, .2f, 1f));
			GUI.DrawTextureWithTexCoords(rightShadowRect, BeamGUI.iconShadowSoftA, new Rect(.8f, 0, .2f, 1f));
			GUI.DrawTextureWithTexCoords(centerShadowRect, BeamGUI.iconShadowSoftA, new Rect(.2f, 0, .6f, 1f));

			GUI.DrawTexture(logoRect, BeamGUI.iconLogoHeader, ScaleMode.ScaleToFit);

		}
	}
}
