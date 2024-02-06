using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost : ISignpostData
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

		public void AfterDeserialize()
		{
			const string packages = "Packages/";
			if (!assetRelativePath.StartsWith(packages)) return;

			var package = assetRelativePath.Replace(packages, string.Empty);
			var stopIndex = package.IndexOf("/", StringComparison.Ordinal);
			package = package.Substring(0, stopIndex);
			var cacheFolder = new DirectoryInfo("Library\\PackageCache");
			var dir = cacheFolder.Exists ? cacheFolder.GetDirectories(package).FirstOrDefault() : null;
			if (dir == null)
			{
				return;
			}

			var result = dir.FullName;
			result = result.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty);
			assetRelativePath = assetRelativePath.Replace($"Packages/{package}", result);
		}
	}
}
