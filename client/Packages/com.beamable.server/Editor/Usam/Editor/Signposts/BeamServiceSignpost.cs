using System;
using System.IO;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost
	{
		public string SolutionPath => Path.Combine(assetRelativePath, relativeProjectFile);
		public string CsprojPath => Path.Combine(assetRelativePath, name);
		public string CsprojFilePath => Path.Combine(assetRelativePath, name, $"{name}.csproj");

		public string name;
		public string assetRelativePath;
		public string relativeDockerFile;
		public string relativeProjectFile;

		public string[] assemblyReferences;
		public string[] dependedStorages;
	}
}
