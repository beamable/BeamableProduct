using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost
	{
		public string SolutionPath => Path.Combine(assetRelativePath, relativeProjectFile);
		public string CsprojPath => Path.Combine(assetRelativePath, name);
		
		public string name;
		public string assetRelativePath;
		public string relativeDockerFile;
		public string relativeProjectFile;

		public string[] assemblyReferences;
		public string[] dependedStorages;

		public static string GetRelativePath(string assetRelativePath)
		{
			const string packages = "Packages/";
			if (!assetRelativePath.StartsWith(packages)) return assetRelativePath;

			var package = assetRelativePath.Replace(packages, string.Empty);
			var stopIndex = package.IndexOf("/", StringComparison.Ordinal);
			package = package.Substring(0, stopIndex);
			var cacheFolder = new DirectoryInfo("Library\\PackageCache");
			var dir = cacheFolder.Exists ? cacheFolder.GetDirectories(package).FirstOrDefault() : null;
			if (dir == null)
			{
				return assetRelativePath;
			}

			var result = dir.FullName;
			result = result.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty);
			result = assetRelativePath.Replace($"Packages/{package}", result);
			return result;
		}
	}
}
