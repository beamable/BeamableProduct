using Beamable.Editor.Util;
using Beamable.Server.Editor;
using System;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.PublishWindow
{
	public partial class UsamPublishWindow
	{
		private Vector2 _contentScroll;

		void DrawPlanUI()
		{
			bool clickedReview = false;

			{
				var text = _planPromise.IsCompleted
					? "Continue to review the release plan."
					: "Building and planning a release plan...";
				DrawHeader(text);
			}

			DrawConfigurationWarnings();

			{
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					margin = new RectOffset(padding, padding, 0, 0),
				});
				_contentScroll = Vector2.zero;
				_contentScroll = EditorGUILayout.BeginScrollView(_contentScroll, false, false);
				EditorGUILayout.Space(10, expand: false);

				foreach (var progressKvp in _planProgressNameToRatio)
				{
					var progressName = progressKvp.Key;
					var value = progressKvp.Value;

					{
						var failedMessage = string.Empty;
						if (value.IsService && _failedServices.Contains(value.serviceName))
						{
							failedMessage = $"<color=#{ColorUtility.ToHtmlStringRGBA(loadingFailed)}>(failed, please see logs)</color>";
						}

						var failed = !string.IsNullOrEmpty(failedMessage);

						var progressLabel = progressName;
						if (failed)
						{
							progressLabel = $"<color=#{ColorUtility.ToHtmlStringRGBA(loadingFailed)}>{progressLabel}</color>";
						}
						DrawLoadingBar(progressLabel, value.progress.ratio,
									   failed: failed,
									   drawBelowLoadingBarUI: () =>
									   {
										   if (!failed) return;
										   //
										   var rect = GUILayoutUtility.GetLastRect();
										   rect = new Rect(rect.x, rect.y + rect.height + 2, rect.width, EditorGUIUtility.singleLineHeight);
										   var style = new GUIStyle(EditorStyles.miniLabel)
										   {
											   richText = true
										   };
										   // style.te
										   EditorGUI.SelectableLabel(rect, failedMessage, style);
									   });
					}
					EditorGUILayout.Space(10, expand: false);
				}

				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();

			}

			DoFlexHeight();

			DrawManifestComment();

			{ // render the action buttons
				{
					EditorGUILayout.BeginHorizontal(new GUIStyle
					{
						padding = new RectOffset(padding, padding, 0, 0)
					});

					GUILayout.FlexibleSpace();

					var btnStyle = new GUIStyle(GUI.skin.button)
					{
						padding = new RectOffset(6, 6, 6, 6)
					};

					isCancelPressed = GUILayout.Button("Cancel", btnStyle);

					GUI.enabled = _planPromise.IsCompleted;
					if (_failedServices.Count > 0)
					{
						isCancelPressed |= GUILayout.Button("Review", btnStyle);
					}
					else
					{
						clickedReview = BeamGUI.CustomButton(new GUIContent("Review"), _primaryButtonStyle);
					}
					GUI.enabled = true;
					EditorGUILayout.EndHorizontal();

				}

				EditorGUILayout.Space(15, expand: false);

				{
					if (clickedReview)
					{
						state = State.REVIEW;
					}
				}
			}

		}
	}
}
