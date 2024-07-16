using Beamable.Common;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableMicroservicesSettingsProvider : SettingsProvider
	{
		class Styles
		{
			public static GUIContent definitions = new GUIContent("Service Assembly Definitions");
		}

		private SerializedObject _customSettings;
		private string _serviceName;

		public BeamableMicroservicesSettingsProvider(string serviceName,
		                                             string path,
		                                             SettingsScope scopes,
		                                             IEnumerable<string> keywords = null) : base(path, scopes, keywords)
		{
			_serviceName = serviceName;
		}

		[SettingsProviderGroup]
		public static SettingsProvider[] CreateMicroservicesSettingsProvider()
		{
			var allProviders = new List<SettingsProvider>();
			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();

			foreach (var definition in codeService.ServiceDefinitions)
			{
				if (!definition.ExistLocally || definition.ServiceType != ServiceType.MicroService)
				{
					continue;
				}

				var provider =
					new BeamableMicroservicesSettingsProvider(definition.BeamoId, "Project/Beamable Services/" + definition.BeamoId,
					                                          SettingsScope.Project)
					{
						keywords = new HashSet<string>(new[] { "Microservice", definition.BeamoId})
					};
				provider.activateHandler += MicroserviceHandler;
				allProviders.Add(provider);
			}

			return allProviders.ToArray();
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			_customSettings = BeamableMicroservicesSettings.GetSerializedSettings(_serviceName);
		}

		public override void OnGUI(string searchContext)
		{
			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_customSettings.FindProperty("assemblyReferences"), Styles.definitions);


			if (GUILayout.Button("Save changes", GUILayout.Width(100)))
			{
				var settings = ((BeamableMicroservicesSettings)_customSettings.targetObject);
				if (!settings.CheckAllValidAssemblies(out string message))
				{
					Debug.LogError($"Error: {message}");
					//TODO also show something in the editor
				}
				else
				{
					settings.SaveChanges().Then((_) =>
					{
						Repaint();
					});
				}
			}

			_customSettings.ApplyModifiedProperties();
		}

		private static void MicroserviceHandler(string searchContext, VisualElement rootElement)
		{
			rootElement.AddStyleSheet($"{Constants.Features.Config.BASE_UI_PATH}/ConfigWindow.uss");
		}
	}

	[Serializable]
	public class BeamableMicroservicesSettings : ScriptableObject
	{
		public List<AssemblyDefinitionAsset> assemblyReferences;

		public string serviceName;

		public bool CheckAllValidAssemblies(out string validationMessage)
		{

			//Check if there is any null reference in the array
			foreach (AssemblyDefinitionAsset assembly in assemblyReferences)
			{
				if (assembly == null)
				{
					validationMessage = "There is a null assembly reference in the list of references";
					return false;
				}
			}

			List<string> names = assemblyReferences.Select(rf => rf.name).ToList();

			//Check if there are duplicates in the list
			if (names.Count != names.Distinct().Count())
			{
				validationMessage = "There are duplicates of a assembly reference in the list";
				return false;
			}

			//Check if that reference is a reference that we can add to the microservice
			foreach (var referenceName in names)
			{
				if (!CsharpProjectUtil.IsValidReference(referenceName))
				{
					validationMessage = $"Assembly reference: {referenceName} is not valid";
					return false;
				}
			}

			validationMessage = string.Empty;
			return true;
		}

		public async Promise SaveChanges()
		{
			await BeamEditorContext
			    .Default.ServiceScope.GetService<CodeService>()
			    .UpdateServiceReferences(serviceName, assemblyReferences);
		}

		public static SerializedObject GetSerializedSettings(string serviceName)
		{
			var codeService = BeamEditorContext
				.Default.ServiceScope.GetService<CodeService>();
			var sd = codeService.ServiceDefinitions.FirstOrDefault(s => s.BeamoId.Equals(serviceName));

			if (sd == null)
			{
				Debug.LogError($"The service: {serviceName} was not found in the available services list");
				return null;
			}

			var instance = ScriptableObject.CreateInstance<BeamableMicroservicesSettings>();
			instance.serviceName = serviceName;

			instance.assemblyReferences = new List<AssemblyDefinitionAsset>();
			foreach (var name in sd.AssemblyDefinitionsNames)
			{
				var guids = AssetDatabase.FindAssets($"{name} t:{nameof(AssemblyDefinitionAsset)}");
				AssemblyDefinitionAsset asset = null;
				foreach (var id in guids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(id);
					var nameQuery = $"{Path.DirectorySeparatorChar}{name}.asmdef";
					if (!assetPath.Contains(nameQuery))
					{
						continue;
					}

					asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);
				}
				instance.assemblyReferences.Add(asset);
			}

			return new SerializedObject(instance);
		}
	}
}
