using System;
using System.IO;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamStorageSignpost : ISignpostData
	{
		public string CsprojPath => assetProjectPath;
		public string CsprojFilePath => Path.Combine(assetProjectPath, $"{name}.csproj");

		public string name;
		public string assetProjectPath = "";
		public void AfterDeserialize(string filePath)
		{
			var directoryPath = Path.GetDirectoryName(filePath);
			if (directoryPath == null)
			{
				Debug.LogError("Failed to find file path.");
				return;
			}

			var path = Path.Combine(directoryPath, "StandaloneMicroservices~/");
			assetProjectPath = Path.Combine(path, assetProjectPath);
		}
	}
}
