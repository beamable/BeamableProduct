using System;
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
	}
}
