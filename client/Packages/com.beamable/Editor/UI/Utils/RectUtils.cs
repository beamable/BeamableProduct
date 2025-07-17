using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI2.Utils
{
	public static class RectUtils
	{
		public static Rect ClampToScreen(this Rect rect)
		{
			Rect editorRect = EditorGUIUtility.GetMainWindowPosition();
            
			// Right edge check
			if (rect.xMax > editorRect.xMax)
				rect.x -= rect.xMax - editorRect.xMax;
            
			// Bottom edge check
			if (rect.yMax > editorRect.yMax)
				rect.y -= rect.yMax - editorRect.yMax;
            
			// Left edge check
			if (rect.x < editorRect.x)
				rect.x = editorRect.x;
                
			// Top edge check
			if (rect.y < editorRect.y)
				rect.y = editorRect.y;
                
			return rect;
		}
		
		public static Rect GetRectProperPosition(this Rect buttonRect)
		{
			Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));
            
			// Ensure the window stays on screen
			Rect screenRect = new Rect(screenPos, buttonRect.size);
			screenRect = screenRect.ClampToScreen();
            
			return screenRect;
		}
	}
}
