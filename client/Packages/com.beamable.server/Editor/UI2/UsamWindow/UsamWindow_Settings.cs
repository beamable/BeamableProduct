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
			if (hasSerializedSettingsYet) return;
			// annoying, Unity's own SerializedObject type is not inherently serializable; so it needs to get
			//  re-serialized over and over again as we domain relaod. 
			serviceSettings.Clear();
			for (var i = 0; i < usam.latestManifest.services.Count; i++)
			{
				var service = usam.latestManifest.services[i];
				var settings = BeamableMicroservicesSettings.GetSerializedSettings(service);
				serviceSettings.Add(settings);
			}
			hasSerializedSettingsYet = true;
		}
		
		void DrawSettings()
		{
			
			var hasServices = usam?.latestManifest?.services?.Count > 0;
			if (!hasServices)
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
			
			{ // draw a little explanation... 
				const string settingHelp = 
					"Configure the settings for the service. ";

				const string noServiceFound =
					"Storage objects have no configurable settings. Please select a Microservice instead. ";
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					padding = new RectOffset(12, 12, 8, 8),
					margin = new RectOffset(12, 12, 12, 12),
				});
				EditorGUILayout.LabelField(foundService == null ? noServiceFound : settingHelp, new GUIStyle(EditorStyles.label)
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
					return;
				}

				{ // temp routing settings
					EditorGUILayout.LabelField($"Session {settings.serviceName} Settings", new GUIStyle(EditorStyles.largeLabel));
					EditorGUILayout.Separator();

					EditorGUI.indentLevel++;

					DrawHelpBox("<b>Editor Only</b> This option will <i>reset everytime you exit Unity!</i>. " +
					            "When there are multiple versions of the service running, such as a deployed version, " +
					            "a local version, or a friendly developer running a service on their machine, then " +
					            "Unity Playmode needs to know which service to use for development. The routing mode controls " +
					            "where your Playmode session will send Microservice requests. " +
					            "\n\n" +
					            "This setting resets after you quit Unity!" +
					            "" );
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
					// GUILayout.Label($"{settings.Key}:", serviceLabelStyle);
					EditorGUILayout.Separator();
					
					DrawHelpBox("In order to access a Storage Object, it must be referenced a dependency. " +
					            "The data is stored in the service's <i>csproj</i> file as a <i>ProjectReference</i> " +
					            "element. Once a Storage Object is listed as a dependency, the service can access " +
					            "it through the <i>Storage</i> accessor, and the service will be given a secure " +
					            "connection-string to the Storage Object on startup. " );
					EditorGUILayout.PropertyField(
						serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.storageDependencies)),
						new GUIContent("Storage Dependencies"));
					EditorGUILayout.Separator();
					
					DrawHelpBox("Unity Assembly Definitions can be shared with the service by listing them " +
					            "as dependencies. A shared assembly definition will generate a dotnet8 compatible " +
					            "<i>csproj</i> file and list it as a <i>ProjectReference</i> in the service's " +
					            "<i>csproj</i> file. Once an assembly definition is listed as a dependency, the code " +
					            "in the assembly may be used freely from the service. " );
					EditorGUILayout.PropertyField(
						serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.assemblyReferences)),
						new GUIContent("Assembly Definitions"));
					EditorGUILayout.Separator();
					
					DrawHelpBox("TODO: Fill out these settings for federation" );
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
						if (!settings.CheckAllValidAssemblies(out var errorMessage))
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
			
			// EditorGUILayout.Space(100);
			void DrawHelpBox(string message)
			{
			
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					padding = new RectOffset(0, 12, 8, 8),
					margin = new RectOffset(24, 12, 12, 12),
				});
				EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label)
				{
					padding = new RectOffset(0,0,0,0),
					margin = new RectOffset(0,0,0,0),
					richText = true,
					wordWrap = true
				});
				EditorGUILayout.EndVertical();
			}
		}
	}
}
