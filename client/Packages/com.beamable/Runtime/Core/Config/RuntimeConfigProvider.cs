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
		public string alias;

		public string host;
		public string portalUrl;
	}

	public class ConfigDatabaseProvider : IRuntimeConfigProvider
	{
		public const string CONFIG_DEFAULTS_NAME = "config-defaults";

		public string Cid => data?.cid;
		public string Pid => data?.pid;
		public string HostUrl
		{
			get
			{	
				return data?.host ?? "https://api.beamable.com";
			}
		}

		public string PortalUrl => data?.portalUrl ?? "https://portal.beamable.com";

		public bool HasNoHostField => string.IsNullOrEmpty(data?.host);
		
		private ConfigData data = GetConfigData();

		public static string GetFullPath(string fileName = null) =>
			Path.Combine("Assets", "Beamable", "Resources", $"{fileName ?? CONFIG_DEFAULTS_NAME}.txt");

		public static ConfigData GetConfigData()
		{
			var json = GetFileContent(CONFIG_DEFAULTS_NAME);
			return JsonUtility.FromJson<ConfigData>(json);
		}

		public static string GetFileContent(string fileName)
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

		public static bool HasConfigFile(string filename = null)
		{
			filename ??= CONFIG_DEFAULTS_NAME;
			// this is hardly efficient, but if it is done infrequently enough, it should be fine
#if UNITY_EDITOR
			return File.Exists(GetFullPath(filename)) || Resources.Load<TextAsset>(filename) != null;
#else
			return Resources.Load<TextAsset>(filename) != null;
#endif
		}

		public void Reload()
		{
			data = GetConfigData();
		}
	}
}
