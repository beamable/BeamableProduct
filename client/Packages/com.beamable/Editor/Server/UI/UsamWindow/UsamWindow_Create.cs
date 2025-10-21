using Beamable.Common.Semantics;
using Beamable.Editor.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		// public bool isCreatingService = false;
		public string newServiceName = "";
		public List<string> newItemDependencies = new List<string>();

		private const string textCreateNewServiceFirst =
			"To get started, you should create a new Beamable Microservice! " +
			"Microservices are dotnet projects that you can use to create cloud code " +
			"for your game. The source code will be saved to your Unity project folder. " +
			"\n\n" +
			"Every Microservice needs a name. ";
		
		private const string textCreateNewStorageFirst =
			"A Storage Object is a Mongo database you can use to manage custom game " +
			"state. The database is represented by a local dotnet project that will " +
			"be saved to your local Unity project folder. " +
			"\n\n" +
			"Every storage object needs a name.";

		private const string textCreateNewService = 
			"Additional Microservices are best when you want to group your cloud code " +
			"by how often you expect it to be invoked. " +
			"\n\n" +
			"Enter a name for the new Microservice. ";
		
		private const string textCreateNewStorage = 
			"Additional Storage Objects are best when you need to completely segregate " +
			"data. " +
			"\n\n" +
			"Enter a name for the new Storage Object. ";

		private const string textSelectStorageDeps = 
			"Microservices may depend on Storage Objects. A Storage Objct is a mongo " +
			"database you can use to manage custom game state. " +
			"\n\n" +
			"[Optional] Select which Storage Objects should be linked to this Microservice. ";

		private const string textSelectServiceDeps = 
			"Storage Objects are only accessible by a Microservice. " +
			"\n\n" +
			"[Optional] Select which Microservices should be linked to this Storage Object. ";

		private const string FederationTemplatePath = "Packages/com.beamable/Editor/Server/UI/ScriptTemplates/FederationId.cs.txt";
		private const string CommonPath = "Assets/Beamable/Common/";
		private const string ScriptNameReplaceTag = "#SCRIPTNAME#";
		private const string ScriptNameLowerCaseReplaceTag = "#SCRIPTNAME_LOWER#";

		private string newFederationIdName = string.Empty;

		
		void DrawNewStorage()
		{
			var firstStorage = usam?.latestManifest?.storages?.Count == 0;
			var deps = usam?.latestManifest?.services?.Select(x => x.beamoId).ToList();
			DrawNew(noun: "Storage", 
			        description: firstStorage ? textCreateNewStorageFirst : textCreateNewStorage,
			        depDescription: textSelectServiceDeps,
			        availableDependencyBeamoIds: deps,
			        onCreate: (name, deps) =>
			        {
				        CheckDocker("create a Storage Object", () =>
				        {
					        var _ = usam.CreateStorage(name, deps);
				        }, out var cancelled);
				        return !cancelled;
			        });
		}

		void DrawNewService()
		{
			var firstService = usam?.latestManifest?.services?.Count == 0;
			var deps = usam?.latestManifest?.storages?.Select(x => x.beamoId).ToList();
			
			DrawNew(noun: "Service", 
				description: firstService ? textCreateNewServiceFirst : textCreateNewService,
				depDescription: textSelectStorageDeps,
				availableDependencyBeamoIds: deps,
				onCreate: (name, deps) =>
				{
					var _ = usam.CreateService(name, deps);
					return true;
				});
		}

		private void DrawNewFederationId()
		{
			EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
			{
				padding = new RectOffset(12, 12, 12, 12),
				margin = new RectOffset(10, 10, 10, 10)
			});

			EditorGUILayout.TextArea("A Federation Id is required in order to add federation to your service.\n\n" +
			                         "Enter the name of your Federation Id below:", new GUIStyle(EditorStyles.label) {wordWrap = true});

			EditorGUILayout.Space(4, false);

			newFederationIdName =
				BeamGUI.LayoutPlaceholderTextField(newFederationIdName, $"[Federation Id Name]", EditorStyles.textField);

			EditorGUILayout.Space(4, false);

			EditorGUILayout.BeginHorizontal(new GUIStyle
			{
				margin = new RectOffset(0, 0, 12, 12)
			});

			EditorGUILayout.Space(5, true);
			EditorGUILayout.Space(5, true);
			var clickedCancel = false;
			clickedCancel = BeamGUI.CancelButton();

			GUI.enabled = !string.IsNullOrEmpty(newFederationIdName) &&
			              !usam.latestManifest.existingFederationIds.Contains(newFederationIdName);
			var clickedCreate = BeamGUI.PrimaryButton(new GUIContent($"Create New Federation"));
			GUI.enabled = true;

			if (clickedCancel)
			{
				newFederationIdName = string.Empty;
				state = WindowState.NORMAL;
			}

			if (clickedCreate)
			{
				AddDelayedAction(() =>
				{
					var template = File.ReadAllText(FederationTemplatePath);
					var lowerCaseName = newFederationIdName;
					if ( !string.IsNullOrEmpty(lowerCaseName) && char.IsUpper(lowerCaseName[0]))
						lowerCaseName = lowerCaseName.Length == 1 ? char.ToLower(lowerCaseName[0]).ToString() : char.ToLower(lowerCaseName[0]) + lowerCaseName[1..];

					var newContent = template.Replace(ScriptNameReplaceTag, newFederationIdName)
					                         .Replace(ScriptNameLowerCaseReplaceTag, lowerCaseName);

					var newFilePath = CommonPath + $"{newFederationIdName}.cs";
					File.WriteAllText(newFilePath, newContent);
					newFederationIdName = string.Empty;

					usam._dispatcher.Run("script-reload", ReloadScripts());

					IEnumerator ReloadScripts()
					{
						while (!File.Exists(newFilePath))
						{
							yield return new WaitForSecondsRealtime(0.5f);
						}
						state = WindowState.NORMAL;
						ActivateSettings();
						AssetDatabase.Refresh();
					}
				});
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
		
		
		void DrawNew(string noun, 
		             string description, 
		             string depDescription, 
		             List<string> availableDependencyBeamoIds,
		             Func<string, List<string>, bool> onCreate)
		{
			{
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					padding = new RectOffset(12, 12, 12, 12),
					margin = new RectOffset(10, 10, 10, 10)
				});

				{ // describe making a new thingy...
					EditorGUILayout.TextArea(description, new GUIStyle(EditorStyles.label) {wordWrap = true});
				}
				
				EditorGUILayout.Space(4, false);
				// note, in Unity 6.2, the height of `EditorStyles.textField` increased, so we need to manually specify line height here. 
				newServiceName =
					BeamGUI.LayoutPlaceholderTextField(newServiceName, $"[{noun} Name]", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

				
				var isValidServiceName = true;
				string newServiceNameError = null;

				{ // validate the service name :( 
					try
					{
						// good heckin' this is a crummy thing.
						var _ = new ServiceName(newServiceName);
					}
					catch (Exception ex)
					{
						newServiceNameError = ex.Message;
					}

					if (newServiceName.Length == 0)
					{
						newServiceNameError = "";
					}
					// TODO: add validations for existing services
					
					isValidServiceName = newServiceNameError == null;

					if (!isValidServiceName && !string.IsNullOrEmpty(newServiceNameError))
					{
						var errorStyle = new GUIStyle(EditorStyles.miniLabel);
						errorStyle.wordWrap = true;
						errorStyle.active.textColor = errorStyle.focused.textColor = errorStyle.hover.textColor = errorStyle.normal.textColor = new Color(1f, .3f, .25f, 1);
						EditorGUILayout.SelectableLabel($"{newServiceNameError.Replace(nameof(ServiceName), $"{noun}Name")}", errorStyle);
					}
				}

				
				{ // draw the dependencies 
					if (availableDependencyBeamoIds?.Count > 0)
					{
						EditorGUILayout.TextArea(depDescription, new GUIStyle(EditorStyles.label)
						{
							wordWrap = true,
							padding = new RectOffset(0, 0, 12, 4)
						});

						EditorGUILayout.BeginVertical(new GUIStyle
						{
							padding = new RectOffset(6, 12, 4, 4)
						});

						var labelStyle = new GUIStyle(EditorStyles.label)
						{
							alignment = TextAnchor.MiddleLeft,
							padding = new RectOffset(0, 0, 4, 4),
						};
						for (var i = 0; i < availableDependencyBeamoIds.Count; i++)
						{
							var depBeamoId = availableDependencyBeamoIds[i];
					
							EditorGUILayout.BeginHorizontal();
							
							var labelRect = GUILayoutUtility.GetRect(new GUIContent(depBeamoId), labelStyle);
							var rowRect = new Rect(labelRect.x - 8, labelRect.y, labelRect.width + 39, 24);
							EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, (i%2 == 0) ? .1f : .2f));
							EditorGUI.LabelField(labelRect, depBeamoId, labelStyle);

							var isDependency = newItemDependencies.Contains(depBeamoId);
							var shouldBeDependency = BeamGUI.LayoutToggle(isDependency, toggleSize:16, yShift: 6);

							if (shouldBeDependency && !isDependency)
							{
								newItemDependencies.Add(depBeamoId);
							} else if (!shouldBeDependency && isDependency)
							{
								newItemDependencies.Remove(depBeamoId);
							}
							
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndVertical();
					}
				}

				{ // draw the buttons
					EditorGUILayout.BeginHorizontal(new GUIStyle
					{
						margin = new RectOffset(0, 0, 12, 12)
					});

					EditorGUILayout.Space(5, true);
					EditorGUILayout.Space(5, true);

					var clickedCancel = false;
					if (cards.Count > 0)
					{
						// can only cancel if there are other services to look at. Otherwise; whats the point?
						clickedCancel = BeamGUI.CancelButton();
					}

					GUI.enabled = isValidServiceName;
					var clickedCreate = BeamGUI.PrimaryButton(new GUIContent($"Create {noun}"));
					GUI.enabled = true;
					if (clickedCancel)
					{
						newServiceName = "";
						state = WindowState.NORMAL;
					}

					if (clickedCreate)
					{
						AddDelayedAction(() =>
						{
							var creationHappened = onCreate.Invoke(newServiceName, newItemDependencies);
							if (creationHappened)
							{
								selectedBeamoId = newServiceName;
								newServiceName = "";
								newItemDependencies.Clear();
								state = WindowState.NORMAL;
							}
							
						});
					}
					
					EditorGUILayout.EndHorizontal();

				}
				
			
				EditorGUILayout.EndVertical();
			}

		}
	}
}
