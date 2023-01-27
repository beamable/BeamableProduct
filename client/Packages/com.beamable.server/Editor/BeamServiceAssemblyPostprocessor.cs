using JetBrains.Annotations;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class BeamServiceAssemblyPostprocessor : AssetPostprocessor
	{
		const string EXTENSION = "asmdef";

		[UsedImplicitly]
		static void OnPostprocessAllAssets(string[] importedAssets,
										   string[] deletedAssets,
										   string[] movedAssets,
										   string[] movedFromAssetPaths,
										   bool didDomainReload)
		{
			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			foreach (string importedAsset in importedAssets)
			{
				if (!importedAsset.EndsWith(EXTENSION))
				{
					continue;
				}

				var dirPath = Path.GetDirectoryName(importedAsset);

				MicroserviceDescriptor descriptor = null;
				for (int i = 0; i < serviceRegistry.Descriptors.Count; i++)
				{
					if (serviceRegistry.Descriptors[i].SourcePath.Contains(dirPath) &&
						BeamServicesCodeWatcher.Default.IsServiceDependedOnStorage(serviceRegistry.Descriptors[i]))
					{
						descriptor = serviceRegistry.Descriptors[i];
						break;
					}
				}

				if (descriptor == null)
				{
					return;
				}

				var asset = descriptor.ConvertToAsset();
				if (!asset.HasMongoLibraries())
				{
					Debug.LogError($"<b>{descriptor.Name}</b> is depended on storage, but is missing dependencies, adding it now.");
					asset.AddMongoLibraries();
				}
			}
		}
	}
}
