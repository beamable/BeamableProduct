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
			for (var i = 0; i < paths.Length; i++)
			{
				if (!PackageUtil.DoesFileExistLocally(paths[i]))
				{
					paths[i] = null; // don't save this one.
					invalidCount++;
				}
			}
			Debug.Log("okay, invalid count " + invalidCount);
			return paths;
		}
	}
}
