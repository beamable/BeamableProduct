using Beamable.Editor.Microservice.UI2.Configs;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		public List<SerializedObject> serviceSettings = new List<SerializedObject>();
		public List<SerializedObject> storageSettings = new List<SerializedObject>();
		public Vector2 settingsScrollPosition;
		[NonSerialized]
		// this is a flag that we'll use to re-serialize the settings when Unity domain-reloads
		private bool hasSerializedSettingsYet = false;

		private List<GUIContent> _routingOptionContents = new List<GUIContent>();
		
		void ActivateSettings()
		{
			state = WindowState.SETTINGS;
			hasSerializedSettingsYet = false;
		}

		void ReserializeSettings()
		{

			if (hasSerializedSettingsYet)
			{
				if (HasInvalidTargetObjects())
				{
					hasSerializedSettingsYet = false;
				}
				else
				{
					// we already serialized, AND the targetObjects appear to still be valid.
					return;
				}
			}
			
			
			// annoying, Unity's own SerializedObject type is not inherently serializable; so it needs to get
			//  re-serialized over and over again as we domain relaod. 
			serviceSettings.Clear();
			for (var i = 0; i < usam.latestManifest.services.Count; i++)
			{
				var service = usam.latestManifest.services[i];
				var settings = BeamableMicroservicesSettings.GetSerializedSettings(service, usam.allAssemblyAssets);
				serviceSettings.Add(settings);
			}

			storageSettings.Clear();
			for (var i = 0; i < usam.latestManifest.storages.Count; i++)
			{
				var storage = usam.latestManifest.storages[i];
				var settings = BeamableStorageSettings.GetSerializedSettings(storage);
				storageSettings.Add(settings);
			}

			hasSerializedSettingsYet = true;

			bool HasInvalidTargetObjects()
			{
				// double check that the serializedObjects we have are still intact.
				//  they may have null'd out during a Playmode transition. 
				foreach (var serializedObject in serviceSettings)
				{
					if (serializedObject.targetObject == null)
					{
						return true;
					}
				}
				foreach (var serializedObject in storageSettings)
				{
					if (serializedObject.targetObject == null)
					{
						return true;
					}
				}

				return false;
			}
		}
		
		void DrawSettings()
		{
			
			var hasServices = usam?.latestManifest?.services?.Count > 0;
			var hasStorages = usam?.latestManifest?.storages?.Count > 0;
			if (!hasServices && !hasStorages)
			{
				EditorGUILayout.LabelField("Please create a service. Until a service exists, there are no settings to configure.", new GUIStyle(EditorStyles.label)
				{
					wordWrap = true
				});
				return;
			}
			
			ReserializeSettings();

			// find the service
			SerializedObject foundService = null;
			for (var i = 0; i < serviceSettings.Count; i++)
			{
				foundService = serviceSettings[i];
				var settings = (BeamableMicroservicesSettings)foundService.targetObject;
				if (settings == null) continue;
				
				if (settings.serviceName != selectedBeamoId)
				{
					foundService = null;
					continue;
				}
				break;
			}

			SerializedObject foundStorage = null;
			if (foundService == null)
			{
				for (var i = 0; i < storageSettings.Count; i++)
				{
					foundStorage = storageSettings[i];
					var settings = (BeamableStorageSettings)foundStorage.targetObject;
					if (settings == null) continue;

					if (settings.storageName != selectedBeamoId)
					{
						foundStorage = null;
						continue;
					}
					break;
				}
			}
			
			{ // draw a little explanation... 
				const string serviceSettingHelp =
					"Configure the settings for the service. ";

				const string storageSettingHelp =
					"Configure the settings for the storage. ";

				const string notFoundText =
					"This should never happens. Something went terrible wrong!";

				string textToShow;

				if (foundService != null)
				{
					textToShow = serviceSettingHelp;
				}else if (foundStorage != null)
				{
					textToShow = storageSettingHelp;
				}
				else
				{
					textToShow = notFoundText;
				}

				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					padding = new RectOffset(12, 12, 8, 8),
					margin = new RectOffset(12, 12, 12, 12),
				});
				EditorGUILayout.LabelField(textToShow, new GUIStyle(EditorStyles.label)
				{
					padding = new RectOffset(0,0,0,0),
					margin = new RectOffset(0,0,0,0),
					richText = true,
					wordWrap = true
				});
				EditorGUILayout.EndVertical();
			}
	
			if (foundService != null)
			{ // draw the selected service settings

				settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);
				EditorGUILayout.BeginVertical();
				DrawServiceSettings(foundService);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}

			if (foundStorage != null)
			{// draw the selected storage settings

				settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);
				EditorGUILayout.BeginVertical();
				DrawStorageSettings(foundStorage);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}
		}

	

		void DrawServiceSettings(SerializedObject serializedObj)
		{
			{
				EditorGUILayout.BeginVertical(new GUIStyle()
				{
					padding = new RectOffset(12, 12, 12, 12)
				});
				

				var settings = (BeamableMicroservicesSettings)serializedObj.targetObject;
				if (settings == null)
				{
					EditorGUILayout.LabelField("Please refresh this page.");
					EditorGUILayout.EndVertical();
					return;
				}

				{ // temp routing settings
					EditorGUILayout.LabelField($"Session {settings.serviceName} Settings", new GUIStyle(EditorStyles.largeLabel));
					EditorGUILayout.Separator();

					EditorGUI.indentLevel++;

					var selectedDisplay = "";
					if (usam.TryGetRoutingSetting(settings.serviceName, out var routingSetting))
					{
						selectedDisplay = routingSetting.selectedOption?.display ?? "(unavailable)";
					}
					
					_routingOptionContents.Clear();
					var selectedRoutingIndex = 0;
					if (usam.TryGetRoutingSetting(settings.serviceName, out var routeSettings))
					{
						// foreach (var option in routeSettings.options)
						for (var i = 0 ; i < routeSettings.options.Count; i ++)
						{
							var option = routeSettings.options[i];
							
							if (option.routingKey == routeSettings.selectedOption.routingKey &&
							    option.type == routeSettings.selectedOption.type)
							{
								selectedRoutingIndex = i;
							}
							switch (option.type)
							{
								case RoutingOptionType.AUTO:
									_routingOptionContents.Add(new GUIContent($"Automatic ({option.display})"));
									break;
								case RoutingOptionType.LOCAL:
									_routingOptionContents.Add(new GUIContent("Only Local"));
									break;
								case RoutingOptionType.REMOTE:
									_routingOptionContents.Add(new GUIContent("Only Realm"));
									break;
								case RoutingOptionType.FRIEND:
									_routingOptionContents.Add(new GUIContent($"Hosted by {option.instance.startedByAccountEmail}"));
									break;
							}
						}
						
						var nextRouteSettingIndex = EditorGUILayout.Popup(new GUIContent("Routing Mode"), selectedRoutingIndex, _routingOptionContents.ToArray());
						routeSettings.selectedOption = routeSettings.options[nextRouteSettingIndex];
					}
					else
					{
						BeamGUI.ShowDisabled(false, () =>
						{
							EditorGUILayout.Popup(new GUIContent("Routing Mode"), 0, new string[] {"(unavailable)"});
						});
					}
					
					EditorGUI.indentLevel--;

				}
				
				EditorGUILayout.Space(30, false);


				{ // reference settings
					EditorGUILayout.LabelField($"{settings.serviceName} Settings",
					                           new GUIStyle(EditorStyles.largeLabel));
					EditorGUI.indentLevel++;
					EditorGUILayout.Separator();
					EditorGUILayout.Separator();
					
					EditorGUILayout.PropertyField(
						serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.storageDependencies)),
						new GUIContent("Storage Dependencies"));
					EditorGUILayout.Separator();
					
					EditorGUILayout.PropertyField(
						serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.assemblyReferences)),
						new GUIContent("Assembly Definitions"));
					EditorGUILayout.Separator();
					
					EditorGUILayout.LabelField("This Federations list is just for view-only. It is a representation of what federations are being implemented on your Microservice class.", new GUIStyle(EditorStyles.label) { wordWrap = true });
					EditorGUILayout.PropertyField(
						serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.federations)), 
						new GUIContent("Federations"), true);
					
					EditorGUILayout.Separator();
					
					serializedObj.ApplyModifiedPropertiesWithoutUndo();
		
					EditorGUI.indentLevel--;
				}
				
				
				if (settings.HasChanges())
				{
					EditorDebouncer.Debounce("save-beam-settings", () =>
					{
						if (!BeamableMicroservicesSettings.CheckAllValidAssemblies(settings.assemblyReferences, out var errorMessage))
						{
							Debug.LogError("error " + errorMessage);
						}
						else if (!settings.CheckAllValidFederations())
						{
							// continue...
						}
						else
						{
							Debug.Log("auto saving settings...");
							settings.SaveChanges(usam).Then(_ =>
							{
								Debug.Log("Saved!");

								usam.WaitReload().Then(_ =>
								{
									// once the manifest is re-read, reserialize our own date!
									hasSerializedSettingsYet = false;
								});
							});
						}
					}, .1f);
				}

				
				EditorGUILayout.EndVertical();
			}
			
		}

		void DrawStorageSettings(SerializedObject serializedObj)
		{
			EditorGUILayout.BeginVertical(new GUIStyle()
			{
				padding = new RectOffset(12, 12, 12, 12)
			});


			var settings = (BeamableStorageSettings)serializedObj.targetObject;
			if (settings == null)
			{
				EditorGUILayout.LabelField("Please refresh this page.");
				EditorGUILayout.EndVertical();
				return;
			}

			{
				// reference settings
				EditorGUILayout.LabelField($"{settings.storageName} Settings",
				                           new GUIStyle(EditorStyles.largeLabel));
				EditorGUI.indentLevel++;
				EditorGUILayout.Separator();
				EditorGUILayout.Separator();

				EditorGUILayout.PropertyField(
					serializedObj.FindProperty(nameof(BeamableStorageSettings.assemblyReferences)),
					new GUIContent("Assembly Definitions"));
				EditorGUILayout.Separator();
			}

			serializedObj.ApplyModifiedPropertiesWithoutUndo();

			if (settings.HasChanges())
			{
				EditorDebouncer.Debounce("save-beam-settings", () =>
				{
					if (!BeamableMicroservicesSettings.CheckAllValidAssemblies(settings.assemblyReferences, out var errorMessage))
					{
						Debug.LogError("error " + errorMessage);
					}
					else
					{
						Debug.Log("auto saving settings...");
						settings.SaveChanges(usam).Then(_ =>
						{
							Debug.Log("Saved!");

							usam.WaitReload().Then(_ =>
							{
								// once the manifest is re-read, reserialize our own date!
								hasSerializedSettingsYet = false;
							});
						});
					}
				}, .1f);
			}


			EditorGUILayout.EndVertical();
		}
	}
}
