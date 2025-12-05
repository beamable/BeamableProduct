using Beamable.Common.Util;
using Beamable.Editor.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.PublishWindow
{
	public partial class UsamPublishWindow
	{
		private Dictionary<string, bool> reviewToFoldout = new Dictionary<string, bool>();
		
		void DrawReviewUi(bool startedUpload)
		{
			var clickedRelease = false;
			var clickedPortal = false;
			
			var anyChanges = _planMetadata.plan.changeCount > 0;
			{ // title label
				
				DrawHeader(anyChanges 
				           ? "Clicking Release will perform the following changes on this realm."
				           : "There are no changes. Clicking Release will restart all existing services. ");
			}
			
			DrawConfigurationWarnings();
			
			if (anyChanges)
			{
				_contentScroll = EditorGUILayout.BeginScrollView(_contentScroll, false, false);
				{
					// show the diff changes

					EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
					{
						margin = new RectOffset(padding, padding, 0, 0)
					});
					EditorGUILayout.Space(10, expand: false);

					var helpUrl = DocsPageHelper.GetUnityDocsPageUrl("unity/user-reference/cloud-services/microservices/microservice-framework/#publishing-microservices", EditorConstants.UNITY_CURRENT_DOCS_VERSION);
					DrawChangeList("Adding Services", "Toolbar Plus", "Learn about new services on Beamable", helpUrl,
					               _planMetadata.plan.diff.addedServices.ToList());
					DrawChangeList("Removing Services", "Warning@2x",
					               "Learn about how services are archived on Beamable", helpUrl,
					               _planMetadata.plan.diff.removedServices.ToList());
					DrawChangeList("Disabling Services", "ProfilerColumn.WarningCount",
					               "Learn about how services can be disabled on Beamable", helpUrl,
					               _planMetadata.plan.diff.disabledServices.ToList());
					DrawChangeList("Enabling Services", "Info@2x",
					               "Learn about how services can be enabled on Beamable", helpUrl,
					               _planMetadata.plan.diff.enabledServices.ToList());
					DrawChangeList("Changing Services", "Info@2x", "Learn about how services changes work on Beamable",
					               helpUrl,
					               _planMetadata.plan.diff.serviceImageIdChanges.Select(x => x.service).ToList());
					
					DrawChangeList("Adding Federations", "Toolbar Plus", "Learn about how services changes work on Beamable",
					               helpUrl,
					               _planMetadata.plan.diff.addedFederations.Select(x => $"{x.service} [{x.federationInterface}/{x.federationId}]").ToList());
					DrawChangeList("Disabling Federations", "Warning@2x", "Learn about how services changes work on Beamable",
					               helpUrl,
					               _planMetadata.plan.diff.removedFederations.Select(x => $"{x.service} [{x.federationInterface}/{x.federationId}]").ToList());

					DrawChangeList("Adding Storage", "Toolbar Plus", "Learn about new storages on Beamable", helpUrl,
					               _planMetadata.plan.diff.addedStorage.ToList());
					DrawChangeList("Removing Storage", "Warning@2x", "Learn about removing storages from Beamable",
					               helpUrl, _planMetadata.plan.diff.removedStorage.ToList());
					DrawChangeList("Disabling Storage", "ProfilerColumn.WarningCount",
					               "Learn about how storages can be disabled on Beamable", helpUrl,
					               _planMetadata.plan.diff.disabledStorages.ToList());
					DrawChangeList("Enabling Storage", "Info@2x", "Learn about how storages can be enabled on Beamable",
					               helpUrl, _planMetadata.plan.diff.enabledStorages.ToList());

					
					{ // review the portal link
						if (EditorGUILayout.LinkButton("Review current deployment"))
						{
							clickedPortal = true;
						}
						EditorGUILayout.Space(4, false);
					}
					
					EditorGUILayout.EndVertical();

				}

				{
					// show upload section
					DrawHeader("The following steps will be completed when you click the Release button.");
				}

				{
					// for services that are uploading, folks can change the comment
					EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
					{
						margin = new RectOffset(padding, padding, 0, 0)
					});

					foreach (var serviceToUpload in _planMetadata.plan.servicesToUpload)
					{
						{
							// show the progress bar
							EditorGUILayout.Space(10, expand: false);

							var planRow = _releaseProgressToRatio
							              .FirstOrDefault(x => x.Value.serviceName == serviceToUpload).Value;

							DrawLoadingBar($"Uploading {serviceToUpload} ", planRow?.progress.ratio ?? 0);
						}
					}

					{
						// a loading bar for the manifest 
						if (!_releaseProgressToRatio.TryGetValue("publish", out var planRow))
						{
							planRow = null;
						}

						EditorGUILayout.Space(10, expand: false);
						DrawLoadingBar("Committing Release", planRow?.progress.ratio ?? 0);
					}

					EditorGUILayout.Space(10, expand: false);
					EditorGUILayout.EndVertical();
					EditorGUILayout.Space(5, expand: false);
				}
				
				EditorGUILayout.EndScrollView();
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

					if (startedUpload)
					{
						if (_releasePromise.IsCompleted)
						{
							isCancelPressed = GUILayout.Button("Okay", btnStyle);
						}
						else
						{
							isCancelPressed = GUILayout.Button("Cancel", btnStyle);
						}

						GUI.enabled = _releasePromise.IsCompleted;
						clickedPortal = BeamGUI.CustomButton(new GUIContent("View in Portal"), _primaryButtonStyle);
						GUI.enabled = true;
					}
					else
					{
						isCancelPressed = GUILayout.Button("Cancel", btnStyle);
						clickedRelease = BeamGUI.CustomButton(new GUIContent("Release"), _primaryButtonStyle);
					}

					EditorGUILayout.EndHorizontal();
					
				}
				
				EditorGUILayout.Space(15, expand: false);

				{
					if (clickedRelease)
					{
						StartRelease();
					}

					if (clickedPortal)
					{
						var url = $"{BeamableEnvironment.PortalUrl}/{_ctx.BeamCli.Cid}/games/{_ctx.BeamCli.ProductionRealm.Pid}/realms/{_ctx.BeamCli.Pid}/microservices?refresh_token={_ctx.Requester.Token.RefreshToken}";
						Application.OpenURL(url);
						
						if (_releasePromise?.IsCompleted ?? false)
						{
							isCancelPressed = true;
						}
					}
				}
			}
		}

		void DrawChangeList(string title, string iconName, string tooltip, string helpUrl, List<string> list)
		{
			if (list.Count == 0)
			{
				return;
			}

			if (!reviewToFoldout.TryGetValue(title, out var foldOut))
			{
				foldOut = true;
			}

			var iconContent = EditorGUIUtility.IconContent(iconName);
			var foldoutContent = new GUIContent(title + $" ({list.Count})");
			var headerRect = GUILayoutUtility.GetRect(foldoutContent, EditorStyles.foldout);
			// foldOut = reviewToFoldout[title] = EditorGUILayout.Foldout(foldOut, new GUIContent(title + $"({list.Count})"), foldOut, EditorStyles.foldout);
			var foldoutRect = new Rect(headerRect.x + headerRect.height, headerRect.y, headerRect.width - headerRect.height * 2,
			                           headerRect.height);
			var iconRect = new Rect(headerRect.x, headerRect.y, headerRect.height, headerRect.height);
			GUI.DrawTexture(iconRect, iconContent.image);
			foldOut = reviewToFoldout[title] = EditorGUI.Foldout(foldoutRect, foldOut, foldoutContent, foldOut, EditorStyles.foldout);

			// var headerRect = GUILayoutUtility.GetLastRect();
			var helpArea = new Rect(headerRect.x + headerRect.width - headerRect.height, headerRect.y, headerRect.height, headerRect.height);
			var helpIcon = EditorGUIUtility.IconContent("_Help");
			helpIcon.tooltip = tooltip;
			var helpClick = GUI.Button(helpArea, helpIcon, new GUIStyle(EditorStyles.iconButton)
			{
				margin = new RectOffset(4, 4, 4, 4)
			});
			if (helpClick)
			{
				Application.OpenURL(helpUrl);
			}
			
			if (foldOut)
			{
				EditorGUI.indentLevel+=2;
				for (var i = 0; i < list.Count; i++)
				{
					var elem = list[i];
					EditorGUILayout.SelectableLabel(elem, EditorStyles.label, GUILayout.Height(20));
				}
				EditorGUI.indentLevel-=2;
			}
			
			
			EditorGUILayout.Space(10, expand: false);
			
		}
	}
}
