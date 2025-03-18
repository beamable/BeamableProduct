using Beamable.Common;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Utility
{
	public static class BeamUnityFileUtils
	{
#if UNITY_WEBGL
		public static Promise<FileInfo> WriteString(string path, string content)
		{
			File.WriteAllText(path, content);
			return Promise<FileInfo>.Successful(new FileInfo(path));
		}

		public static Promise<FileInfo> WriteBytes(string path, byte[] content)
		{
			File.WriteAllBytes(path, content);
			return Promise<FileInfo>.Successful(new FileInfo(path));
		}
		
		public static Promise<string> ReadStringFile(string path)
		{
			var content = File.ReadAllText(path);
			return Promise<string>.Successful(content);
		}

		public static Promise<byte[]> ReadBytesFile(string path)
		{
			var content = File.ReadAllBytes(path);
			return Promise<byte[]>.Successful(content);
		}
#else
		/// <summary>
		/// Writes a string to a file asynchronously.
		/// </summary>
		public static async Promise<FileInfo> WriteStringFile(string path, string content)
		{
			await File.WriteAllTextAsync(path, content);
			return new FileInfo(path);
		}

		/// <summary>
		/// Writes a byte array to a file asynchronously.
		/// </summary>
		public static async Promise<FileInfo> WriteBytesFile(string path, byte[] content)
		{
			await File.WriteAllBytesAsync(path, content);
			return new FileInfo(path);
		}

		/// <summary>
		/// Reads a file as a string asynchronously.
		/// </summary>
		public static async Promise<string> ReadStringFile(string path)
		{
			return await File.ReadAllTextAsync(path);
		}

		/// <summary>
		/// Reads a file as a byte array asynchronously.
		/// </summary>
		public static async Promise<byte[]> ReadBytesFile(string path)
		{
			return await File.ReadAllBytesAsync(path);
		}
#endif

		/// <summary>
		/// Moves a file to an archive folder, appending a timestamp to its filename.
		/// <para>This method is recommended over direct deletion to prevent accidental data loss.</para>
		/// </summary>
		public static void ArchiveFile(string filePath, string archivePath)
		{
			if (!File.Exists(filePath))
			{
				Debug.LogWarning($"File {filePath} does not exist");
				return;
			}

			// Adds a suffix to indicate the archive date
			string fileName = $"{Path.GetFileName(filePath)}-Archive-{DateTime.UtcNow:yyyyMMddHHmmss}";
			string archivedFilePath = Path.Combine(archivePath, fileName);
			File.Move(filePath, archivedFilePath);
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
			return new FileInfo(newFilePath);
		}

		/// <summary>
		/// Serializes an object to JSON and writes it to a file.
		/// <para>If no custom serializer is provided, Unity's <see cref="JsonUtility.ToJson(object)"/> will be used.</para>
		/// </summary>
		/// <typeparam name="T">Type of the object to serialize.</typeparam>
		/// <param name="path">File path where the JSON will be saved.</param>
		/// <param name="content">Object to serialize.</param>
		/// <param name="serializer">Optional custom serializer function.</param>
		/// <param name="prettyPrint">Whether to format the JSON for readability.</param>
		/// <returns>A <see cref="Promise"/> resolving to a <see cref="FileInfo"/> after writing is complete.</returns>
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
		/// <para>If no custom deserializer is provided, Unity's <see cref="JsonUtility.FromJson{T}(string)"/> will be used.</para>
		/// </summary>
		/// <typeparam name="T">Type of the object to deserialize.</typeparam>
		/// <param name="path">File path from which JSON will be read.</param>
		/// <param name="deserializer">Optional custom deserializer function.</param>
		/// <param name="defaultObject">Object to return if the file does not exist.</param>
		/// <returns>A <see cref="Promise"/> resolving to the deserialized object.</returns>
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
			using FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			int numBytesToRead = Convert.ToInt32(fs.Length);
			byte[] fileBytes = new byte[numBytesToRead];
			fs.Read(fileBytes, 0, numBytesToRead);

			return fileBytes;
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
	}
}

