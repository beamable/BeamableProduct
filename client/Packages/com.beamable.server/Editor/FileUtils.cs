
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public static class FileUtils
	{
	
	
		public static void DeleteDirectoryRecursively(string path)
		{
			foreach (string directory in Directory.GetDirectories(path))
			{
				DeleteDirectoryRecursively(directory);
			}

			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException)
			{
				Directory.Delete(path, true);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(path, true);
			}
		}
	}
}
