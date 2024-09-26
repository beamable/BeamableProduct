using Beamable.Common;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableMicroservicesSettingsProvider : SettingsProvider
	{
		class Styles
		{
			public static GUIContent definitions = new GUIContent("Service Assembly Definitions");
			public static GUIContent dependencies = new GUIContent("Storages Dependencies");
		}

		private SerializedObject _customSettings;
		private string _serviceName;
		private int _selected;
		private bool _showDependenciesEditor;
		private bool _confirmLeaveTriggered = false;

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

			if (BeamEditorContext.Default == null)
			{
				return allProviders.ToArray();
			}

			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();

			foreach (var definition in codeService.ServiceDefinitions)
			{
				if (definition.ServiceType != ServiceType.MicroService)
				{
					continue;
				}

				var provider =
					new BeamableMicroservicesSettingsProvider(definition.BeamoId, "Project/Beamable Services/" + definition.BeamoId,
															  SettingsScope.Project)
					{
						keywords = new HashSet<string>(new[] { "Microservice", definition.BeamoId })
					};
				allProviders.Add(provider);
			}

			return allProviders.ToArray();
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			_confirmLeaveTriggered = false;

			_customSettings = BeamableMicroservicesSettings.GetSerializedSettings(_serviceName);

			//load all possible dependencies
			var codeService = BeamEditorContext
							  .Default.ServiceScope.GetService<CodeService>();

			var options = codeService.ServiceDefinitions.Where(sd => sd.ServiceType == ServiceType.StorageObject)
									 .Select(sd => sd.BeamoId).ToArray();

			_showDependenciesEditor = options.Length > 0;

			rootElement.AddStyleSheet($"{Constants.Features.Config.BASE_UI_PATH}/ConfigWindow.uss");
		}

		public override void OnDeactivate()
		{
			if (_customSettings == null)
			{
				return;
			}

			var settings = ((BeamableMicroservicesSettings)_customSettings.targetObject);

			if (!_confirmLeaveTriggered && settings.HasChanges())
			{
				_confirmLeaveTriggered = true;
				if (EditorUtility.DisplayDialog("Unsaved Changes",
												"You have unsaved changes! Would you like to save them before leaving?",
												"Yes", "No"))
				{
					TrySaveChanges(settings);
				}
			}
		}

		public override void OnGUI(string searchContext)
		{
			EditorGUILayout.Separator();
			if (_showDependenciesEditor)
			{
				EditorGUILayout.PropertyField(_customSettings.FindProperty(nameof(BeamableMicroservicesSettings.storageDependencies)), Styles.dependencies);
			}
			else
			{
				EditorGUILayout.LabelField("There are no storages yet created!");
			}

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_customSettings.FindProperty(nameof(BeamableMicroservicesSettings.assemblyReferences)), Styles.definitions);
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			// CHeck if thre are changes or not
			var settings = ((BeamableMicroservicesSettings)_customSettings.targetObject);

			GUI.enabled = settings.HasChanges();
			if (GUILayout.Button("Save changes", GUILayout.Width(100)))
			{
				TrySaveChanges(settings);
			}

			_customSettings.ApplyModifiedProperties();
		}

		public static void TrySaveChanges(BeamableMicroservicesSettings settings)
		{
			if (!settings.CheckAllValidAssemblies(out string message))
			{
				Debug.LogError($"Error: {message}");
				//TODO also show something in the editor
			}
			else
			{
				settings.SaveChanges();
			}
		}
	}

	[Serializable]
	public class BeamableMicroservicesSettings : ScriptableObject
	{

		public List<AssemblyDefinitionAsset> assemblyReferences;
		private List<AssemblyDefinitionAsset> originalAssemblyReferences = new List<AssemblyDefinitionAsset>();

		public List<StorageDependency> storageDependencies;
		private List<StorageDependency> originalStorageDependencies = new List<StorageDependency>();

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

		public bool HasChanges()
		{
			if (originalAssemblyReferences == null || assemblyReferences == null ||
				originalStorageDependencies == null || storageDependencies == null)
			{
				return false;
			}

			if (!ScrambledEquals(originalAssemblyReferences, assemblyReferences))
				return true;
			return !ScrambledEquals(originalStorageDependencies, storageDependencies);
		}

		public void SaveChanges()
		{
			UpdateOriginalData();
			var dependencies = storageDependencies.Select(dep => dep.StorageName).ToList();
			_ = BeamEditorContext
				.Default.ServiceScope.GetService<CodeService>()
				.SetMicroserviceChanges(serviceName, assemblyReferences, dependencies);
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

			var dependencies = sd.Dependencies.Select(dp => new StorageDependency() { StorageName = dp }).ToList();
			instance.storageDependencies = dependencies;

			instance.UpdateOriginalData();

			return new SerializedObject(instance);
		}

		public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
		{
			return list1.All(item => list2.Contains(item)) && list1.Distinct().Count() == list1.Count() && list1.Count() == list2.Count();
		}

		private void UpdateOriginalData()
		{
			originalAssemblyReferences.Clear();
			originalStorageDependencies.Clear();
			assemblyReferences.ForEach(asmdef => originalAssemblyReferences.Add(asmdef));
			storageDependencies.ForEach(dep => originalStorageDependencies.Add(dep));
		}

		[Serializable]
		public class StorageDependency
		{
			public string StorageName;
		}

		[CustomPropertyDrawer(typeof(StorageDependency))]
		public class StorageDependencyDrawer : PropertyDrawer
		{
			private int _selected;
			private Regex _regex = new Regex(".Array.data\\[([0-9]+)]");

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//load all possible dependencies
				var codeService = BeamEditorContext
								  .Default.ServiceScope.GetService<CodeService>();
				var options = codeService.ServiceDefinitions.Where(sd => sd.ServiceType == ServiceType.StorageObject)
										 .Select(sd => sd.BeamoId).ToArray();

				var storageNameProperty = property.FindPropertyRelative(nameof(StorageDependency.StorageName));

				//Some stuff to get the index of this property in it's array
				string indexInArray = string.Empty;
				if (property.propertyPath.Contains("Array"))
				{
					indexInArray =
						_regex.Match(property.propertyPath).Groups[1].ToString();
				}

				var previousIndex = Array.IndexOf(options, storageNameProperty.stringValue);

				var index = EditorGUI.Popup(position, $"Element {indexInArray}", previousIndex, options);
				storageNameProperty.stringValue = index >= 0 ? options[index] : "None";
			}
		}
	}
}
