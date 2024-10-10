using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Beamable.Editor.Util
{

	public partial class BeamGUI
	{
		
		
		public static bool CustomButton(GUIContent content, GUIStyle style)
		{
			var rect = GUILayoutUtility.GetRect(content, style);
			
			var isHover = rect.Contains(Event.current.mousePosition);
			var buttonClicked = isHover && Event.current.rawType == EventType.MouseDown;

			bool clicked = false;
			clicked = GUI.Button(rect, content, style);

			if (buttonClicked)
			{
			}
			else if (isHover)
			{
				EditorGUI.DrawRect(rect, new Color(1, 1, 1, .1f));
				// EditorGUI.DrawRect(rect, new Color(.25f, .5f, 1f, .5f));
				// var subStyle = new GUIStyle(style);
				// subStyle.normal = subStyle.onNormal = style.onHover;
				// clicked = GUI.Button(rect, content, subStyle);
				
			}
			else
			{
				// EditorGUI.DrawTextureTransparent(rect, style.onNormal.background);

				// clicked = GUI.Button(rect, content, style);
			}
			return clicked;
		}
		
		
		public static GUIStyle ColorizeButton(Color color, float mix = .8f)
		{
			var style = ColorizeStyle(GUI.skin.button, color, mix);
			// style.hover.background = new Texture2D(style.normal.background.width, style.normal.background.height, style.normal.background.graphicsFormat, style.normal.background.mipmapCount, TextureCreationFlags.MipChain);
			// Graphics.CopyTexture(style.normal.background, style.hover.background);
			//
			// var pix = style.hover.background.GetPixels();
			// for (var i = 0; i < pix.Length; i++)
			// {
			// 	var original = pix[i];
			// 	pix[i] = Color.Lerp(pix[i], Color.white, original.a * .1f);
			// }
			// style.hover.background.SetPixels(pix);
			// style.hover.background.Apply();
			// style.onHover = style.hover;
			
			return style;
		}
		
		public static GUIStyle ColorizeStyle(GUIStyle style, Color color, float mix=.8f)
		{
			var colorized = new GUIStyle(style);

			// colorized.onActive.scaledBackgrounds = ColorizeStateTextures(colorized.active, color, mix);
			// if (colorized.onActive.scaledBackgrounds.Length > 0)
			// 	colorized.onActive.background = colorized.onActive.scaledBackgrounds[0];
			// colorized.active = colorized.onNormal;

			colorized.onNormal.scaledBackgrounds = ColorizeStateTextures(colorized.onNormal, color, mix);
			if (colorized.onNormal.scaledBackgrounds.Length > 0)
				colorized.onNormal.background = colorized.onNormal.scaledBackgrounds[0];
			colorized.normal = colorized.onNormal;
			
			
			colorized.onHover.scaledBackgrounds = ColorizeStateTextures(colorized.normal, Color.Lerp(color, Color.white, .1f), mix);
			if (colorized.onHover.scaledBackgrounds.Length > 0)
				colorized.onHover.background = colorized.onHover.scaledBackgrounds[0];
			colorized.hover = colorized.onNormal;
			//
			// colorized.onFocused.scaledBackgrounds = ColorizeStateTextures(colorized.focused, color, mix);
			// if (colorized.onFocused.scaledBackgrounds.Length > 0)
			// 	colorized.onFocused.background = colorized.onFocused.scaledBackgrounds[0];
			// colorized.focused = colorized.onNormal;

			return colorized;
		}
		
		public static Texture2D[] ColorizeStateTextures(GUIStyleState styleState, Color color, float mix=.8f)
		{
			var backgrounds = styleState.scaledBackgrounds;
			var colorizedBackgrounds = new Texture2D[backgrounds.Length];
			for (var textureIndex = 0; textureIndex < backgrounds.Length; textureIndex++)
			{
						
				var normal = backgrounds[textureIndex];
				colorizedBackgrounds[textureIndex] = new Texture2D(normal.width, normal.height, normal.graphicsFormat, normal.mipmapCount, TextureCreationFlags.MipChain);
				Graphics.CopyTexture(normal, colorizedBackgrounds[textureIndex]);
				var pix = colorizedBackgrounds[textureIndex].GetPixels();
				for (var i = 0; i < pix.Length; i++)
				{
					var original = pix[i];
					pix[i] = Color.Lerp(pix[i], color, original.a * mix);
					// pix[i].a = .3f;
				}
				colorizedBackgrounds[textureIndex].SetPixels(pix);
				colorizedBackgrounds[textureIndex].Apply();
			}

			return colorizedBackgrounds;
		}
	}
}
