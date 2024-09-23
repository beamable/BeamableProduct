using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableMicroserviceSectionSettingsProvider : SettingsProvider
	{
		private bool hasServicesInitialized = false;
		private Vector2 _servicesListPosition;
		private Dictionary<string, SerializedObject> _allCustomSettings;

		class Styles
		{
			public static GUIContent definitions = new GUIContent("Service Assembly Definitions");
			public static GUIContent dependencies = new GUIContent("Storages Dependencies");
		}

		public BeamableMicroserviceSectionSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

		[SettingsProvider]
		public static SettingsProvider CreateMicroservicesSectionProvider()
		{
			var provider =
				new BeamableMicroserviceSectionSettingsProvider("Project/Beamable Services",
																SettingsScope.Project);
			provider.keywords = new HashSet<string>(new[] { "Microservice" });

			return provider;
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			BeamEditorContext.Default.OnReady.Then(async (_) =>
			{
				var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
				await codeService.OnReady;
				hasServicesInitialized = true;
				_allCustomSettings = new Dictionary<string, SerializedObject>();

				foreach (var serviceDefinition in codeService.ServiceDefinitions)
				{
					if (serviceDefinition.ServiceType == ServiceType.StorageObject)
					{
						continue;
					}

					var serviceCustomSetting =
						BeamableMicroservicesSettings.GetSerializedSettings(serviceDefinition.BeamoId);
					_allCustomSettings.Add(serviceDefinition.BeamoId, serviceCustomSetting);
				}

				codeService.OnServicesRefresh -= RefreshData;
				codeService.OnServicesRefresh += RefreshData;
				RefreshData(null);
			});
		}

		public override void OnGUI(string searchContext)
		{
			if (!hasServicesInitialized)
			{
				return;
			}
			GUIStyle serviceLabelStyle = new GUIStyle();
			serviceLabelStyle.fontSize = 15;
			serviceLabelStyle.normal.textColor = Color.white;
			serviceLabelStyle.padding.left = 10;

			_servicesListPosition = EditorGUILayout.BeginScrollView(_servicesListPosition);
			GUILayout.Space(20);
			foreach (var settings in _allCustomSettings)
			{
				EditorGUILayout.Separator();
				GUILayout.Label($"{settings.Key}:", serviceLabelStyle);
				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(settings.Value.FindProperty(nameof(BeamableMicroservicesSettings.storageDependencies)), Styles.dependencies);
				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(settings.Value.FindProperty(nameof(BeamableMicroservicesSettings.assemblyReferences)), Styles.definitions);
				EditorGUILayout.Separator();

				var settingConverted = ((BeamableMicroservicesSettings)settings.Value.targetObject);

				GUI.enabled = settingConverted.HasChanges();
				if (GUILayout.Button("Save changes", GUILayout.Width(100)))
				{
					BeamableMicroservicesSettingsProvider.TrySaveChanges(settingConverted);
				}

				GUI.enabled = true;
				EditorGUILayout.Space(100);

				settings.Value.ApplyModifiedProperties();
			}
			EditorGUILayout.EndScrollView();

		}

		private void RefreshData(List<IBeamoServiceDefinition> _)
		{
			SettingsService.NotifySettingsProviderChanged();
		}
	}
}
