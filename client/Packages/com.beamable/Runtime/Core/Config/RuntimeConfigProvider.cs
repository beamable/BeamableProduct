using Beamable.Common;
using System;
using System.IO;
using UnityEngine;

namespace Beamable.Config
{

	[Serializable]
	public class ConfigData
	{
		public string cid;
		public string pid;
	}
	
	public class ConfigDatabaseProvider : IRuntimeConfigProvider
	{
		private const string CONFIG_DEFAULTS_NAME = "config-defaults";

		public string Cid => data?.cid;
		public string Pid => data?.pid;

		private ConfigData data;

		public ConfigDatabaseProvider()
		{
			var json = GetFileContent(CONFIG_DEFAULTS_NAME);
			data = JsonUtility.FromJson<ConfigData>(json);
		}

		private static string GetFullPath(string fileName) =>
			Path.Combine("Assets", "Beamable", "Resources", $"{fileName}.txt");

		private static string GetFileContent(string fileName)
		{
#if UNITY_EDITOR
			var fullPath = GetFullPath(fileName);

			if (File.Exists(fullPath))
			{
				var result = File.ReadAllText(fullPath);
				return result;
			}
#endif
			var asset = Resources.Load(fileName) as TextAsset;
			if (asset == null)
			{
				return "{}"; // empty json.
				// throw new FileNotFoundException("Cannot find config file in Resource directory", fileName);
			}

			return asset.text;
		}
	}
}
