using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using System.IO;
using UnityEngine;

namespace Beamable.Editor
{
	public class EditorStorageLayer : IStorageLayer
	{
		private readonly IBeamableFilesystemAccessor _fileSystem;

		public EditorStorageLayer(IBeamableFilesystemAccessor fileSystem)
		{
			_fileSystem = fileSystem;
		}

		public void Save<T>(string key, T content)
		{
			var json = JsonUtility.ToJson(content);
			var fileName = GetFileName(key);
			File.WriteAllText(fileName, json);
		}

		public void Apply<T>(string key, T instance)
		{
			var fileName = GetFileName(key);
			if (!File.Exists(fileName)) return;
			var json = File.ReadAllText(fileName);
			JsonUtility.FromJsonOverwrite(json, instance);
		}

		private string GetFileName(string key)
		{
			var path = Path.Combine(_fileSystem.GetPersistentDataPathWithoutTrailingSlash(), "beamable", "editorStorage");
			Directory.CreateDirectory(path);
			var fileName = Path.Combine(path, key) + ".json";
			return fileName;
		}
	}
}
