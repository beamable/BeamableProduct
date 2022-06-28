using Newtonsoft.Json;
using System.Net.Http.Json;

namespace cli;


public class ConfigService
{
	public bool? ConfigFileExists { get; private set; }
	public string? ConfigFilePath { get; private set; }

	private Dictionary<string, string>? _config;

	public ConfigService()
	{
		RefreshConfig();
	}

	public string PrettyPrint() => JsonConvert.SerializeObject(_config, Formatting.Indented);

	public string? GetConfigString(string key, string? defaultValue = null)
	{
		if (_config?.TryGetValue(key, out var value) ?? false)
		{
			return value;
		}

		return defaultValue;
	}

	public string SetConfigString(string key, string value)
	{
		if (_config != null) _config[key] = value;
		return _config[key];
	}

	public void SetBeamableDirectory(string dir)
	{
		ConfigFilePath = Path.Combine(dir, Constants.CONFIG_FOLDER); ;
	}

	public void FlushConfig()
	{
		if (string.IsNullOrEmpty(ConfigFilePath))
			throw new CliException("No beamable project exists. Please use beam init");
		var json = JsonConvert.SerializeObject(_config);
		if (!Directory.Exists(ConfigFilePath))
		{
			Directory.CreateDirectory(ConfigFilePath);
		}
		string fullPath = Path.Combine(ConfigFilePath, Constants.CONFIG_DEFAULTS_FILE_NAME);
		File.WriteAllText(fullPath, json);
	}

	void RefreshConfig()
	{
		ConfigFileExists = TryToFindBeamableConfigFolder(out var configPath);
		ConfigFilePath = configPath;
		TryToReadConfigFile(ConfigFilePath, out _config);
	}

	bool TryToFindBeamableConfigFolder(out string? result)
	{
		result = string.Empty;
		var basePath = Directory.GetCurrentDirectory();
		if (Directory.Exists(Path.Combine(basePath, Constants.CONFIG_FOLDER)))
		{
			result = Path.Combine(basePath, Constants.CONFIG_FOLDER);
			return true;
		}

		var parentDir = Directory.GetParent(basePath);
		while (parentDir != null)
		{
			var path = Path.Combine(parentDir.FullName, Constants.CONFIG_FOLDER);
			if (Directory.Exists(path))
			{
				result = path;
				return true;
			}

			parentDir = parentDir.Parent;
		}

		return false;
	}

	bool TryToReadConfigFile(string? folderPath, out Dictionary<string, string> result)
	{
		string fullPath = Path.Combine(folderPath, Constants.CONFIG_DEFAULTS_FILE_NAME);
		result = new Dictionary<string, string>();
		if (File.Exists(fullPath))
		{
			var content = File.ReadAllText(fullPath);
			result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

			return result is { Count: > 0 };
		}

		return false;
	}
}
