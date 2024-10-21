
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public static class FileUtils
	{
		public static void CleanBuildDirectory(MicroserviceDescriptor descriptor)
		{
			var dirExists = Directory.Exists(descriptor.BuildPath);
#if UNITY_EDITOR_WIN
			var longestPathLength = dirExists ? Directory
			                                    .GetFiles(descriptor.BuildPath, "*", SearchOption.AllDirectories)
			                                    .OrderByDescending(p => p.Length)
			                                    .FirstOrDefault()?.Length : descriptor.BuildPath.Length;
			UnityEngine.Assertions.Assert.IsFalse(longestPathLength + Directory.GetCurrentDirectory().Length >= 260,
			                                     "Project path is too long and can cause issues during building on Windows machine. " +
			                                     "Consider moving project to other folder so the project path would be shorter.");
#endif
			// remove everything in the hidden folder...
			if (dirExists)
			{
				OverrideDirectoryAttributes(new DirectoryInfo(descriptor.BuildPath), FileAttributes.Normal);
				Directory.Delete(descriptor.BuildPath, true);
			}
			Directory.CreateDirectory(descriptor.BuildPath);
		}


		public static string GetFullSourcePath(AssemblyDefinitionInfo assemblyDependency)
		{
			string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
			var sourceDirectory = Path.GetDirectoryName(assemblyDependency.Location);
			var fullSource = Path.Combine(rootPath, sourceDirectory);

			return fullSource;
		}

		public static string GetBuildContextPath(AssemblyDefinitionInfo assemblyDependency)
		{
			return assemblyDependency.Name;
		}

		public static void CopyFolderToBuildDirectory(string sourceFolderPath, string subFolder, MicroserviceDescriptor descriptor)
		{
			var directoryQueue = new Queue<string>();
			directoryQueue.Enqueue(sourceFolderPath);

			while (directoryQueue.Count > 0)
			{
				var path = directoryQueue.Dequeue();

				var files = Directory
				   .GetFiles(path);
				foreach (var file in files)
				{
					var subPath = file.Substring(sourceFolderPath.Length + 1);
					var destinationFile = Path.Combine(descriptor.BuildPath, subFolder, subPath);

#if UNITY_EDITOR_WIN
               var fullPath = Path.GetFullPath(destinationFile);
               if (fullPath.Length >= 255)
               {
                  Debug.LogError($"There could be problems during building {descriptor.Name}- path is too long. " +
                                      "Consider moving project to another folder so path would be shorter.");
               }
#endif

					Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
					File.Copy(file, destinationFile, true);
				}

				var subDirs = Directory.GetDirectories(path);
				foreach (var subDir in subDirs)
				{
					var dirName = Path.GetFileName(subDir);
					if (new[] { "~", "obj", "bin" }.Contains(dirName) || dirName.StartsWith("."))
						continue; // skip hidden or dumb folders...

					directoryQueue.Enqueue(subDir);
				}
			}

		}

		public static void OverrideDirectoryAttributes(DirectoryInfo dir, FileAttributes fileAttributes)
		{
			foreach (var subDir in dir.GetDirectories())
			{
				OverrideDirectoryAttributes(subDir, fileAttributes);
				subDir.Attributes = fileAttributes;
			}

			foreach (var file in dir.GetFiles())
			{
				file.Attributes = fileAttributes;
			}
		}

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
