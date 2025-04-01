using Beamable.Common;
using System;
using System.IO;
using System.Linq;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace Beamable.Utility
{
	public static class BeamUnityFileUtils
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void SyncToIndexedDB();

		public static void PrepareIndexedDB()
		{
			SyncToIndexedDB();
		}

#endif
		/// <summary>
		/// Writes a string to a file asynchronously.
		/// </summary>
		public static async Promise<FileInfo> WriteStringFile(string path, string content)
		{
			return await ExecuteWithErrorHandling(async () =>
			{
#if UNITY_WEBGL && !UNITY_EDITOR
				File.WriteAllText(path, content);
				SyncToIndexedDB();
	            return await Promise<FileInfo>.Successful(new FileInfo(path));
#else
				await File.WriteAllTextAsync(path, content);
				return new FileInfo(path);
#endif

			});
		}

		/// <summary>
		/// Writes a byte array to a file asynchronously.
		/// </summary>
		public static async Promise<FileInfo> WriteBytesFile(string path, byte[] content)
		{
			return await ExecuteWithErrorHandling(async () =>
			{
#if UNITY_WEBGL && !UNITY_EDITOR
	            File.WriteAllBytes(path, content);
				SyncToIndexedDB();
	            return await Promise<FileInfo>.Successful(new FileInfo(path));
#else
				await File.WriteAllBytesAsync(path, content);
				return new FileInfo(path);
#endif
			});
		}

		/// <summary>
		/// Reads a file as a string asynchronously.
		/// </summary>
		public static async Promise<string> ReadStringFile(string path)
		{
			return await ExecuteWithErrorHandling(async () =>
			{
				if (!File.Exists(path))
					return string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
                return await Promise<string>.Successful(File.ReadAllText(path));
#else
				return await File.ReadAllTextAsync(path);
#endif
			});
		}

		/// <summary>
		/// Reads a file as a byte array asynchronously.
		/// </summary>
		public static async Promise<byte[]> ReadBytesFile(string path)
		{
			return await ExecuteWithErrorHandling(async () =>
			{
				if (!File.Exists(path))
					return Array.Empty<byte>();
#if UNITY_WEBGL && !UNITY_EDITOR
                return await Promise<byte[]>.Successful(File.ReadAllBytes(path));
#else
				return await File.ReadAllBytesAsync(path);
#endif
			});
		}


		/// <summary>
		/// Moves a file to an archive folder, appending a timestamp to its filename.
		/// </summary>
		public static void ArchiveFile(string filePath, string archivePath)
		{
			if (!File.Exists(filePath))
			{
				Debug.LogWarning($"File {filePath} does not exist");
				return;
			}

			string fileName = $"{Path.GetFileName(filePath)}-Archive-{DateTime.UtcNow:yyyyMMddHHmmss}";
			string archivedFilePath = Path.Combine(archivePath, fileName);
			File.Move(filePath, archivedFilePath);

#if UNITY_WEBGL && !UNITY_EDITOR
			SyncToIndexedDB();
#endif
		}

		/// <summary>
		/// Renames a file while keeping it in the same directory.
		/// </summary>
		public static FileInfo RenameFile(string filePath, string newFileName)
		{
			string fileDirectoryPath = Path.GetDirectoryName(filePath);
			string newFileNamePath = Path.Combine(fileDirectoryPath, newFileName);
			return MoveFile(filePath, newFileNamePath);
		}

		/// <summary>
		/// Moves a file to a new location, ensuring no overwrite occurs.
		/// </summary>
		public static FileInfo MoveFile(string filePath, string newFilePath)
		{
			if (File.Exists(newFilePath))
			{
				throw new Exception($"Could not move/rename the file. A file with name {newFilePath} already exists.");
			}

			File.Move(filePath, newFilePath);

#if UNITY_WEBGL && !UNITY_EDITOR
			SyncToIndexedDB();
#endif

			return new FileInfo(newFilePath);
		}

		/// <summary>
		/// Serializes an object to JSON and writes it to a file.
		/// </summary>
		public static async Promise<FileInfo> WriteJsonContent<T>(string path,
		                                                          T content,
		                                                          Func<object, string> serializer = null,
		                                                          bool prettyPrint = false)
		{
			string jsonData = serializer == null ? JsonUtility.ToJson(content, prettyPrint) : serializer(content);
			return await WriteStringFile(path, jsonData);
		}

		/// <summary>
		/// Reads and deserializes a JSON file into an object.
		/// </summary>
		public static async Promise<T> ReadJsonContent<T>(string path,
		                                                  Func<string, object> deserializer = null,
		                                                  T defaultObject = default)
		{
			if (File.Exists(path))
			{
				string content = await ReadStringFile(path);
				object deserializedObject =
					deserializer == null ? JsonUtility.FromJson<T>(content) : deserializer(content);
				if (deserializedObject != null)
					return (T)deserializedObject;
			}

			return defaultObject;
		}

		/// <summary>
		/// Reads a file without locking it, allowing concurrent access.
		/// <para>This is useful for scenarios where multiple processes or threads might need to read the file.</para>
		/// </summary>
		public static byte[] ReadFileWithoutLock(string filename)
		{
			using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				int numBytesToRead = Convert.ToInt32(fs.Length);
				byte[] fileBytes = new byte[numBytesToRead];
				fs.Read(fileBytes, 0, numBytesToRead);

				return fileBytes;
			}
		}

		/// <summary>
		/// Cleans a filename by replacing invalid path characters.
		/// </summary>
		/// <param name="fileName">The original filename.</param>
		/// <param name="replaceChar">Character to replace invalid characters with. Default is '_'.</param>
		/// <returns>A sanitized filename that is safe to use in paths.</returns>
		public static string SanitizeFileName(string fileName, char replaceChar = '_')
		{
			if (string.IsNullOrEmpty(fileName))
				return fileName;

			char[] invalidChars = Path.GetInvalidPathChars();
			return new string(fileName.Select(c => invalidChars.Contains(c) ? replaceChar : c).ToArray());
		}

		// Default error handling for both WebGL and non-WebGL environments
		private static async Promise<T> ExecuteWithErrorHandling<T>(Func<Promise<T>> action)
		{
			try
			{
				return await action();
			}
			catch (UnauthorizedAccessException e)
			{
				Debug.LogError($"Unauthorized access: {e.Message}");
				return default;
			}
			catch (IOException e)
			{
				Debug.LogError($"IO error: {e.Message}");
				return default;
			}
			catch (Exception e)
			{
				Debug.LogError($"Unexpected error: {e.Message}");
				return default;
			}
		}
	}
}
