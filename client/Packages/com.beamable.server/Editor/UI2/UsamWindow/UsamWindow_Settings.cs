using Beamable.Editor.Microservice.UI2.Configs;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2
	{
		public List<SerializedObject> serviceSettings = new List<SerializedObject>();
		public Vector2 settingsScrollPosition;
		[NonSerialized]
		// this is a flag that we'll use to re-serialize the settings when Unity domain-reloads
		private bool hasSerializedSettingsYet = false;
		
		
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
				EditorGUILayout.TextArea("Please create a service. Until a service exists, there are no settings to configure.", new GUIStyle(EditorStyles.label)
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
					padding = new RectOffset(2, 2, 8, 8),
					margin = new RectOffset(12, 12, 12, 12)
				});
				EditorGUILayout.TextArea(foundService == null ? noServiceFound : settingHelp, new GUIStyle(EditorStyles.label)
				{
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

				EditorGUILayout.LabelField($"{settings.serviceName} Settings", new GUIStyle(EditorStyles.largeLabel));

				EditorGUI.indentLevel++;
				EditorGUILayout.Separator();
				// GUILayout.Label($"{settings.Key}:", serviceLabelStyle);
				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(
					serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.storageDependencies)),
					new GUIContent("Storage Dependencies"));
				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(
					serializedObj.FindProperty(nameof(BeamableMicroservicesSettings.assemblyReferences)),
					new GUIContent("Assembly Definitions"));
				EditorGUILayout.Separator();
				serializedObj.ApplyModifiedProperties();

				if (settings.HasChanges())
				{
					
					if (!settings.CheckAllValidAssemblies(out var errorMessage))
					{
						Debug.LogError("error " + errorMessage);
					}
					else
					{
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
				}
				EditorGUI.indentLevel--;

				
				EditorGUILayout.EndVertical();
			}
			
			// EditorGUILayout.Space(100);

		}
	}
}
