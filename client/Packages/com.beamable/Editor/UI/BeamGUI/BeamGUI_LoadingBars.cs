using System;
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
		const int loadingBarLabelWidth = 100;

		public static void DrawLoadingBar(string label, float value, bool failed = false, GUIStyle labelStyleBase = null, Action drawBelowLoadingBarUI = null)
		{
			EditorGUILayout.BeginHorizontal(new GUIStyle
			{
				// leave some room for a loading indicator... 
				padding = new RectOffset(0, 30, 0, 0)
			});
			var labelStyle = new GUIStyle(labelStyleBase ?? EditorStyles.miniLabel)
			{
				alignment = TextAnchor.UpperRight,
				wordWrap = true,
				richText = true
			};


			EditorGUILayout.BeginVertical(GUILayout.Width(loadingBarLabelWidth));
			EditorGUILayout.LabelField(new GUIContent(label), labelStyle, GUILayout.MaxWidth(loadingBarLabelWidth), GUILayout.Width(loadingBarLabelWidth));
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

			// reserve a rect that acts as top padding
			GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
									 GUILayout.Height(2),
									 GUILayout.ExpandWidth(true));
			var progressRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
													 GUILayout.Height(5),
													 GUILayout.ExpandWidth(true));

			// reserve a rect that acts as lower padding
			GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
									 GUILayout.Height(2),
									 GUILayout.ExpandWidth(true));

			progressRect = new Rect(progressRect.x, progressRect.y, progressRect.width - 4,
															progressRect.height);
			EditorGUI.DrawRect(progressRect, loadingBackdrop);

			var color = failed ? loadingFailed : loadingPrimary;

			// EditorGUI.DrawRect(GetLoadingRectHorizontal(progressRect, value), color);
			// AddLoadingBarIfLoading(new Rect(progressRect.x, progressRect.y, progressRect.width, progressRect.height), value, failed);

			LoadingRect(progressRect, value, failed, animate: value < .99f);

			drawBelowLoadingBarUI?.Invoke();
			EditorGUILayout.EndVertical();

			var numericRect = new Rect(progressRect.xMax + 4, 2 + progressRect.y - EditorGUIUtility.singleLineHeight * .5f, 30, EditorGUIUtility.singleLineHeight);
			EditorGUI.SelectableLabel(numericRect, value < .01f ? "--" : $"{(value * 100):00}%", new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = new GUIStyleState
				{
					textColor = value < .01f
						? new Color(0, 0, 0, .4f)
						: color
				}
			});

			EditorGUILayout.EndHorizontal();
		}


		public static void LoadingRect(Rect fullRect, float ratio, bool isFailed = false, bool animate = true)
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
