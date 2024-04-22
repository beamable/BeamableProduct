using Beamable.Editor.UI.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

namespace Beamable.Server.Editor.Usam
{
	public static class MigrationHelper
	{

		public static List<string> GetDependentServices(string storageName)
		{
			List<string> allDeps = new List<string>();

			foreach (var serviceModel in MicroservicesDataModel.Instance.Services)
			{
				var storagesModels = GetDependentStorages(serviceModel);

				if (storagesModels.Select(model => model.Name).Contains(storageName))
				{
					allDeps.Add(serviceModel.Name);
				}
			}

			return allDeps;
		}

		public static IEnumerable<MongoStorageModel> GetDependentStorages(MicroserviceModel microserviceModel)
		{
			var microserviceAssemblyDefinition = AssemblyDefinitionHelper.ConvertToInfo(microserviceModel.Descriptor);
			var storageObjectAssemblyDefinitionsAssets = GetAllStorageObjectAssemblyDefinitionAssets();

			var serviceDependencies = new List<MongoStorageModel>();
			foreach (var storageObjectAssemblyDefinitionsAsset in storageObjectAssemblyDefinitionsAssets)
			{
				if (microserviceAssemblyDefinition.References.Contains(AssemblyDefinitionHelper.ConvertToInfo(storageObjectAssemblyDefinitionsAsset.Value).Name))
				{
					serviceDependencies.Add(storageObjectAssemblyDefinitionsAsset.Key);
				}
			}
			return serviceDependencies;
		}

		public static Dictionary<MongoStorageModel, AssemblyDefinitionAsset> GetAllStorageObjectAssemblyDefinitionAssets()
		{
			var storageObjectAssemblyDefinitionsAssets = new Dictionary<MongoStorageModel, AssemblyDefinitionAsset>();
			foreach (var storageObject in MicroservicesDataModel.Instance.Storages)
			{
				var assemblyDefinition = AssemblyDefinitionHelper.ConvertToAsset(storageObject.Descriptor);
				if (assemblyDefinition == null)
					continue;
				storageObjectAssemblyDefinitionsAssets.Add(storageObject, assemblyDefinition);
			}
			return storageObjectAssemblyDefinitionsAssets;
		}
	}
}
