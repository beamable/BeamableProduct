using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Configs
{

	[Serializable]
	public class BeamableMicroservicesSettings : ScriptableObject
	{
		public List<AssemblyDefinitionAsset> assemblyReferences;
		private List<AssemblyDefinitionAsset> originalAssemblyReferences = new List<AssemblyDefinitionAsset>();

		public List<StorageDependency> storageDependencies;
		private List<StorageDependency> originalStorageDependencies = new List<StorageDependency>();

		public string serviceName => service.beamoId;
		public BeamManifestServiceEntry service;

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

			var nonEmptyStorages = storageDependencies.ToList();
			nonEmptyStorages.RemoveAll(x => string.IsNullOrEmpty(x.StorageName));
			
			var nonEmptyAssemblies = assemblyReferences.ToList();
			nonEmptyAssemblies.RemoveAll(x => x==null);

			if (!ScrambledEquals(originalAssemblyReferences, nonEmptyAssemblies))
				return true;
			return !ScrambledEquals(originalStorageDependencies, nonEmptyStorages);
		}

		public Promise SaveChanges(UsamService usam)
		{
			UpdateOriginalData();
			var dependencies = storageDependencies.Select(dep => dep.StorageName).ToList();
			return usam.SetMicroserviceChanges(serviceName, assemblyReferences, dependencies);
		}

		public static SerializedObject GetSerializedSettingsLegacy(string serviceName)
		{
			throw new NotImplementedException();
		}
		
		public static SerializedObject GetSerializedSettings(BeamManifestServiceEntry service)
		{
			var instance = CreateInstance<BeamableMicroservicesSettings>();
			instance.service = service;

			instance.assemblyReferences = new List<AssemblyDefinitionAsset>();
			foreach (var name in service.unityReferences)
			{
				var guids = AssetDatabase.FindAssets($"{name.AssemblyName} t:{nameof(AssemblyDefinitionAsset)}");
				AssemblyDefinitionAsset asset = null;
				foreach (var id in guids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(id);
					var nameQuery = $"{Path.DirectorySeparatorChar}{name.AssemblyName}.asmdef";
					if (!assetPath.Contains(nameQuery))
					{
						continue;
					}
					asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);
				}
				instance.assemblyReferences.Add(asset);
			}

			var dependencies = service.storageDependencies.Select(dp => new StorageDependency() {StorageName = dp}).ToList();
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

			originalStorageDependencies.RemoveAll(x => x.StorageName == null);
			originalAssemblyReferences.RemoveAll(x => x == null);
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
				                  .Default.ServiceScope.GetService<UsamService>();
				var options = codeService.latestManifest.storages
				                         .Select(sd => sd.beamoId).ToArray();

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

				if (index >= 0)
				{
					storageNameProperty.stringValue = options[index];
				}
				else
				{
					storageNameProperty.stringValue = null;
				}
			}
		}
	}
}
