using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost : ISignpostData
	{
		public string CsprojPath => assetProjectPath;
		public string CsprojFilePath => Path.Combine(assetProjectPath, $"{name}.csproj");

		public ServiceType serviceType;
		public string name;
		public string assetProjectPath = "";

		public string[] assemblyReferences;

		public void AfterDeserialize(string filePath)
		{
			var directoryPath = Path.GetDirectoryName(filePath);
			assetProjectPath = Path.Combine(directoryPath, assetProjectPath);
		}
	}
}
