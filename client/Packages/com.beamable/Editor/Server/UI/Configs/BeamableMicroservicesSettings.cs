using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Server;
using Beamable.Server.Editor;
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

		public List<BeamStorageDependencySetting> storageDependencies;
		private List<BeamStorageDependencySetting> originalStorageDependencies = new List<BeamStorageDependencySetting>();

		public BeamFederationSetting federations;

		public static List<string> availableFederationIds = new List<string>();
		
		public string serviceName => service.beamoId;
		public BeamManifestServiceEntry service;

		public bool CheckAllValidFederations()
		{
			return federations.IsValid();
		}
		
		public static bool CheckAllValidAssemblies(List<AssemblyDefinitionAsset> assemblyReferences, out string validationMessage)
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
			var nonEmptyStorages = storageDependencies.ToList();
			nonEmptyStorages.RemoveAll(x => string.IsNullOrEmpty(x.StorageName));
			
			var nonEmptyAssemblies = assemblyReferences.ToList();
			nonEmptyAssemblies.RemoveAll(x => x==null);

			if (!ScrambledEquals(originalAssemblyReferences, nonEmptyAssemblies))
				return true;
			if (!ScrambledEquals(originalStorageDependencies, nonEmptyStorages))
				return true;
			
			return false;
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
		
		public static SerializedObject GetSerializedSettings(BeamManifestServiceEntry service,
		                                                     Dictionary<string, AssemblyDefinitionAsset> allAssemblies)
		{
			var instance = CreateInstance<BeamableMicroservicesSettings>();
			instance.service = service;

			{ // set the available federation Ids
				var federationIdTypes = UsamService.CompiledFederationIds;
				foreach (var fedIdInstance in federationIdTypes)
				{
					availableFederationIds.Add(fedIdInstance.GetUniqueName());
				}
			}

			{ // update the assembly references
				instance.assemblyReferences = new List<AssemblyDefinitionAsset>();
				foreach (var name in service.unityReferences)
				{
					AssemblyDefinitionAsset asset = null;
					foreach (var assemblyDefAsset in allAssemblies)
					{
						if (!name.AssemblyName.Equals(assemblyDefAsset.Value.name))
						{
							continue;
						}

						asset = assemblyDefAsset.Value;
						break;
					}

					if (asset == null) //if the asset is still null, we don't try to add this assembly as reference, put it in a list and ask user to manually add the reference
					{
						Debug.LogError($"The assembly reference {name} could not be added.");
						continue;
					}

					instance.assemblyReferences.Add(asset);
				}
			}

			{ // update the storages
				var dependencies = service.storageDependencies.Select(dp => new BeamStorageDependencySetting() {StorageName = dp})
				                          .ToList();
				instance.storageDependencies = dependencies;
			}

			{ // update the federations
				instance.federations = new BeamFederationSetting()
				{
					entries = service.federations.ToList() // copy the list
				};
			}
			
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

	
	}
}
