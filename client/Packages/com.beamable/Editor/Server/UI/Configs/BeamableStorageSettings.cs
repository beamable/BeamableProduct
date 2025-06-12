using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Server.Editor.Usam;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableStorageSettings : ScriptableObject
	{
		public List<AssemblyDefinitionAsset> assemblyReferences;
		private List<AssemblyDefinitionAsset> originalAssemblyReferences = new List<AssemblyDefinitionAsset>();

		public BeamManifestStorageEntry storage;
		public string storageName => storage.beamoId;


		public static SerializedObject GetSerializedSettings(BeamManifestStorageEntry storage)
		{
			var instance = CreateInstance<BeamableStorageSettings>();
			instance.storage = storage;

			{ // update the assembly references
				instance.assemblyReferences = new List<AssemblyDefinitionAsset>();
				foreach (var name in storage.unityReferences)
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
			}
			instance.UpdateOriginalData();

			return new SerializedObject(instance);
		}

		public Promise SaveChanges(UsamService usam)
		{
			UpdateOriginalData();
			return usam.SetStorageChanges(storageName, assemblyReferences);
		}

		public bool HasChanges()
		{
			if (originalAssemblyReferences == null || assemblyReferences == null)
			{
				return false;
			}

			var nonEmptyAssemblies = assemblyReferences;
			nonEmptyAssemblies.RemoveAll(x => x==null);

			if (!BeamableMicroservicesSettings.ScrambledEquals(originalAssemblyReferences, nonEmptyAssemblies))
				return true;

			return false;
		}

		private void UpdateOriginalData()
		{
			originalAssemblyReferences.Clear();

			assemblyReferences.ForEach(asmdef => originalAssemblyReferences.Add(asmdef));

			originalAssemblyReferences.RemoveAll(x => x == null);
		}
	}
}
