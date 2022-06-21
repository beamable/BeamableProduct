using Newtonsoft.Json;

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

	public string? GetConfigString(string key, string? defaultValue=null)
	{
		if (_config?.TryGetValue(key, out var value) ?? false)
		{
			return value;
		}

		return defaultValue;
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

			return result is {Count: > 0};
		}

		return false;
	}
}