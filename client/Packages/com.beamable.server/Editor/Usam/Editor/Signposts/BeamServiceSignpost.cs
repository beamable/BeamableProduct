using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost : ISignpostData
	{
		public string CsprojPath => assetProjectPath;
		public string CsprojFilePath => Path.Combine(assetProjectPath, $"{name}.csproj");

		public string name;
		public string assetProjectPath = "";

		public AssemblyDefinitionAsset[] assemblyReferences;

		public void AfterDeserialize(string filePath)
		{
			var directoryPath = Path.GetDirectoryName(filePath);
			if (directoryPath == null)
			{
				Debug.LogError("Failed to find file path.");
				return;
			}

			string path = Path.Combine(directoryPath, CodeService.StandaloneMicroservicesFolderName);
			assetProjectPath = Path.Combine(path, assetProjectPath);
		}
		
		public bool CheckAllValidAssemblies()
		{

			//Check if there is any null reference in the array
			foreach (AssemblyDefinitionAsset assembly in assemblyReferences)
			{
				if (assembly == null) return false;
			}

			List<string> names = assemblyReferences.Select(rf => rf.name).ToList();

			//Check if there are duplicates in the list
			if (names.Count != names.Distinct().Count())
			{
				return false;
			}

			//Check if that reference is a reference that we can add to the microservice
			foreach (var referenceName in names)
			{
				if (!CsharpProjectUtil.IsValidReference(referenceName))
				{
					return false;
				}
			}

			return true;
		}
	}
}
