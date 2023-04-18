using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	public class FileModificationWarning : AssetModificationProcessor
	{
		static string[] OnWillSaveAssets(string[] paths)
		{
			Debug.Log("Performing safety check... Brought you by Beamable...");
			var invalidCount = 0;

			var actualPaths = new List<string>(paths.Length);

			paths = paths.Where(PackageUtil.DoesFileExistLocally).ToArray();
			for (var i = 0; i < paths.Length; i++)
			{
				if (PackageUtil.DoesFileExistLocally(paths[i]))
				{
					actualPaths.Add(paths[i]); // don't save this one.
				}
				else
				{
					Debug.Log("Invalidated " + paths[i]);
					invalidCount++;
				}
			}
			Debug.Log("okay, invalid count " + invalidCount);
			return paths;
		}
	}
}
