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
		
		public ServiceType serviceType;
		public string name;
		public string assetProjectPath = "";
		public void AfterDeserialize(string filePath)
		{
			Debug.Log("GABRIEL filePath + " + filePath);
			var directoryPath = Path.GetDirectoryName(filePath);
			if (directoryPath == null)
			{
				Debug.LogError("Failed to find file path.");
				return;
			}
			Debug.Log("GABRIEL dirPath + " + directoryPath);

			var path = Path.Combine(directoryPath, "StandaloneMicroservices~/");
			assetProjectPath = Path.Combine(path, assetProjectPath);
			Debug.Log("GABRIEL projPath + " + assetProjectPath);
		}
	}
}
