using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	/// <summary>
	/// Beamable may have types that try to serialize and save themselves, but they shouldn't. This class will prevent them from attempting to save.
	/// </summary>
	public class FileModificationWarning : UnityEditor.AssetModificationProcessor
	{
		public const string PREFIX = "Packages/com.beamable";
		private static readonly List<string> InvalidPaths = new List<string>();
		private static readonly List<string> NextPaths = new List<string>();

		static string[] OnWillSaveAssets(string[] paths)
		{
			InvalidPaths.Clear();
			foreach (var path in paths) // find the invalid paths
			{
				if (!path.StartsWith(PREFIX)) continue; // if this path isn't even in beamable, don't do anything.

				if (!PackageUtil.DoesFileExistLocally(path)) // if this file does not exist, we cannot save it.
				{
					InvalidPaths.Add(path); // don't save this one.
				}
			}

			if (InvalidPaths.Count > 0)
			{
				NextPaths.Clear();
				NextPaths.AddRange(paths);
				foreach (var path in InvalidPaths)
				{
#if BEAMABLE_LOG_INVALID_ASSET_SAVE_WARNINGS
					Debug.LogWarning("Beamable will not save asset at " + path);
#endif
					NextPaths.Remove(path);
				}

				paths = NextPaths.ToArray();
			}

			return paths;

		}
	}
}
