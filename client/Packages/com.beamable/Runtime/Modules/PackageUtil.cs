using System;
using System.IO;

namespace Beamable
{
	public class PackageUtil
	{

		/// <summary>
		/// When using Unity Packages, the package can be installed locally, or through the Unity package cache.
		/// When the package is installed locally, the package files actually exist in the /Packages/com.package/ folder.
		/// However, when the package is installed through the cache, the files do not exist there. Unity's own asset calls
		/// still use /Package/com.package/ style paths, but they are redirected and proxied.
		///
		/// This method will check for the presence of the package.json file in the given package id.
		/// 
		/// </summary>
		/// <param name="file">Any file path, but if it isn't pointing the /Packages folder, this method isn't recommened. </param>
		/// <returns>
		/// True if the given <see cref="file"/> exists locally, False if the package is not installed, or is installed through the cache.
		/// </returns>
		public static bool DoesFileExistLocally(string file)
		{
			if (!File.Exists(file)) return false;
			file = Path.GetFullPath(file);
			// if the path is relative to the /Library/PackageCache folder, then this file doesn't _really_ exist in the same way we think it does.
			var relativePath = GetRelativePath(System.Environment.CurrentDirectory, file);
			var isInPackageCache = relativePath.StartsWith("Library/PackageCache");
			return !isInPackageCache;
		}

		/// <summary>
		/// In NetCore 2.0, there is a system function for this, but Unity 2019 & 2020 don't support it. So here is
		/// a shim from https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
		public static string GetRelativePath(string fromPath, string toPath)
		{
			if (string.IsNullOrEmpty(fromPath))
			{
				throw new ArgumentNullException("fromPath");
			}

			if (string.IsNullOrEmpty(toPath))
			{
				throw new ArgumentNullException("toPath");
			}

			fromPath = Path.GetFullPath(fromPath);
			toPath = Path.GetFullPath(toPath);
			Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
			Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

			if (fromUri.Scheme != toUri.Scheme)
			{
				return toPath;
			}

			Uri relativeUri = fromUri.MakeRelativeUri(toUri);
			string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
			{
				relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			}

			return relativePath;
		}

		private static string AppendDirectorySeparatorChar(string path)
		{
			// Append a slash only if the path is a directory and does not have a slash.
			if (Directory.Exists(path) &&
				!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				return path + Path.DirectorySeparatorChar;
			}

			return path;
		}
	}
}
