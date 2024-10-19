using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		private static Texture _animationTexture;
		
		private static readonly Color loadingBackdrop = new Color(0, 0, 0, .3f);
		private static readonly Color loadingPrimary = new Color(.25f, .5f, 1f, 1);
		private static readonly Color loadingFailed = new Color(1, .3f, .25f, 1);
		
		public static void LoadingRect(Rect fullRect, float ratio, bool isFailed=false, bool animate=true)
		{
			var fillRect = new Rect(fullRect.x, fullRect.y, fullRect.width * ratio, fullRect.height);

			EditorGUI.DrawRect(fullRect, loadingBackdrop);
			EditorGUI.DrawRect(fillRect, isFailed ? loadingFailed : loadingPrimary);

			if (animate)
			{
				if (_animationTexture == null)
				{
					_animationTexture =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/loading_animation.png");
				}

				{
					var time = (float)((EditorApplication.timeSinceStartup * .3) % 1);
					GUI.DrawTextureWithTexCoords(fillRect, _animationTexture,
					                             new Rect(-time, 0, 1.2f, 1));
				}
			}
		}
	}
}
