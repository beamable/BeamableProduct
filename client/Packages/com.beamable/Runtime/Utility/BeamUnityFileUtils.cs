using Beamable.Common;
using System;
using System.IO;
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
		public static async Promise<FileInfo> WriteStringFile(string path, string content)
		{
			await File.WriteAllTextAsync(path, content);
			return new FileInfo(path);
		}

		public static async Promise<FileInfo> WriteBytesFile(string path, byte[] content)
		{
			await File.WriteAllBytesAsync(path, content);
			return new FileInfo(path);
		}

		public static async Promise<string> ReadStringFile(string path)
		{
			return await File.ReadAllTextAsync(path);
		}

		public static async Promise<byte[]> ReadBytesFile(string path)
		{
			return await File.ReadAllBytesAsync(path);
		}
		
#endif
		
		public static Promise<Unit> ArchiveFile(string filePath, string archivePath)
		{
			if (!File.Exists(filePath))
			{
				// There is no File to Archive, finish Promise early
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			// Generate new File name adding -Archive-<DateWhenWasArchived>
			var fileName = $"{Path.GetFileName(filePath)}-Archive-{DateTime.UtcNow:yyyyMMddHHmmss}";
			string archivedFilePath = Path.Combine(archivePath, fileName); 
			File.Move(filePath,  archivedFilePath);

			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		public static Promise<bool> RenameFile(string filePath, string newFileName)
		{
			var fileDirectoryPath = Path.GetDirectoryName(filePath);
			var extension = Path.GetExtension(filePath);
			string newFileNamePath = $"{Path.Combine(fileDirectoryPath, newFileName)}.{extension}";
			if (File.Exists(newFileNamePath))
			{
				Debug.LogWarning($"There is already a file name {newFileName}.{extension} in directory {fileDirectoryPath}");
				return Promise<bool>.Successful(false);
			}
			File.Move(filePath, newFileNamePath);
			return Promise<bool>.Successful(true);
		}

		
		public static async Promise<FileInfo> WriteJsonContent<T>(string path, T content, Func<object, string> serializer = null, bool prettyPrint = false)
		{
			string jsonData = serializer == null ? JsonUtility.ToJson(content, prettyPrint) : serializer(content);
			return await WriteStringFile(path, jsonData);
		}

		public static async Promise<T> ReadJsonContent<T>(string path, Func<string, object> deserializer = null)
		{
			string content = await ReadStringFile(path);
			object deserializedObject = deserializer == null ? JsonUtility.FromJson<T>(content) : deserializer(content);
			return (T)deserializedObject;
		}
	}
}
