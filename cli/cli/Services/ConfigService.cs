using Beamable.Common;
using Beamable.Common.Api;
using cli.Dotnet;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine.Binding;
using System.Text;

namespace cli;



[Serializable]
public enum Vcs
{
	Git,
	// ReSharper disable once InconsistentNaming
	SVN,
	P4
}

public class ConfigService
{
	private readonly CliEnvironment _environment;
	private readonly ConfigDirOption _configDirOption;
	public string WorkingDirectory => _dir;
	public bool? ConfigFileExists { get; private set; }
	[CanBeNull] public string ConfigFilePath { get; private set; }
	public string BaseDirectory => Path.GetDirectoryName(ConfigFilePath);

	[CanBeNull] private Dictionary<string, string> _config;

	private string _dir;

	public ConfigService(CliEnvironment environment, ConfigDirOption configDirOption)
	{
		_environment = environment;
		_configDirOption = configDirOption;
	}

	public void Init(BindingContext bindingContext)
	{
		if (!TryGetSetting(out _dir, bindingContext, _configDirOption))
		{
			_dir = Directory.GetCurrentDirectory();
		}

		RefreshConfig();
	}

	/// <summary>
	/// by default, paths are relative to the execution working directory...
	/// But you may need them to be relative to the project root.
	///
	/// This function will take a relative directory from the execution site, and turn it into a relative path from the project's root.
	/// The project's root is the folder that _contains_ /.beamable
	/// </summary>
	/// <param name="relativePath"></param>
	/// <returns></returns>
	public string GetRelativePath(string relativePath)
	{
		var rootDir = Directory.GetParent(ConfigFilePath).FullName;
		var fullRoot = Path.GetFullPath(rootDir);

		var path = Path.Combine(fullRoot, relativePath);
		path = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
		return path;
	}

	/// <summary>
	/// Returns the full of that will be make from combining parent directory of a config and a relative path
	/// </summary>
	/// <param name="relativePath">path relative to the config parent folder</param>
	/// <returns></returns>
	public string GetFullPath(string relativePath)
	{
		return Path.Combine(BaseDirectory, relativePath);
	}

	/// <summary>
	/// Gets the full path for a given configuration file.
	/// </summary>
	/// <param name="pathInConfig">The path of the file in the configuration.</param>
	/// <returns>The full path of the file in the configuration.</returns>
	public string GetConfigPath(string pathInConfig)
	{
		if (string.IsNullOrWhiteSpace(ConfigFilePath))
		{
			throw new CliException($"Could not find {pathInConfig} because config is undetected.");
		}
		var basePath = ConfigFilePath;
		if (Constants.TEMP_FILES.Contains(pathInConfig))
		{
			basePath = Path.Combine(basePath, Constants.TEMP_FOLDER);
		}

		return Path.Combine(basePath, pathInConfig);
	}


	public string GetServicesDir(SolutionCommandArgs args, string newSolutionPath)
	{
		string result = string.Empty;
		//using try catch because of the Directory.EnumerateDirectories behaviour
		try
		{
			var list = Directory.EnumerateDirectories(BaseDirectory,
				$"{args.SolutionName}\\services",
				SearchOption.AllDirectories).ToList();
			if (list.Count > 0)
			{
				result = Path.GetRelativePath(BaseDirectory, list.First());
			}
		}
		catch
		{
			//
		}

		try
		{
			if (string.IsNullOrWhiteSpace(result))
			{
				var list = Directory.EnumerateDirectories(newSolutionPath, "services",
					SearchOption.AllDirectories).ToList();
				result = Path.GetRelativePath(BaseDirectory, list.First());
			}
		}
		catch
		{
			//
		}

		if (string.IsNullOrWhiteSpace(result))
		{
			const string SERVICES_PATH_ERROR = "Could not find Solution services path!";
			Log.Error(SERVICES_PATH_ERROR);
		}

		return result;
	}

	public void SaveDataFile<T>(string fileName, T data)
	{
		if (string.IsNullOrWhiteSpace(ConfigFilePath))
		{
			throw new CliException($"Could not write {fileName} because config is undetected.");
		}

		var json = JsonConvert.SerializeObject(data,
			new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented});
		var file = GetConfigPath(fileName);
		File.WriteAllText(file, json);
	}

	public T LoadDataFile<T>(string fileName) where T : new() => LoadDataFile<T>(fileName, () => new T());

	public T LoadDataFile<T>(string fileName, Func<T> defaultValueGenerator)
	{
		if (string.IsNullOrWhiteSpace(ConfigFilePath))
		{
			throw new CliException($"Could not write {fileName} because config is undetected.");
		}
		try
		{
			var filePath = GetConfigPath(fileName);
			if (!File.Exists(filePath)) { return defaultValueGenerator(); }

			var json = File.ReadAllText(filePath);
			var data = JsonConvert.DeserializeObject<T>(json,
				new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
			return data;
		}
		catch (Exception ex)
		{
			BeamableLogger.LogError(ex.Message);
			throw;
		}
	}

	public void SetTempWorkingDir(string dir)
	{
		_dir = dir;
		SetBeamableDirectory(_dir);
	}


	public bool TryGetSetting(out string value, BindingContext context, ConfigurableOption option,
		string defaultValue = null)
	{
		// Try to get from option
		value = context.ParseResult.GetValueForOption(option);

		// Try to get from config service
		if (value == null)
			value = GetConfigString(option.OptionName, defaultValue);

		// Try to get from environment service.
		if (string.IsNullOrEmpty(value))
		{
			_ = _environment.TryGetFromOption(option, out value);
			CliSerilogProvider.Instance.Debug(
				$"Trying to get option={option.GetType().Name} from Env Vars! Value Found={value}");
		}

		var hasValue = !string.IsNullOrEmpty(value);
		return hasValue;
	}

	public string PrettyPrint() => JsonConvert.SerializeObject(_config, Formatting.Indented);

	[CanBeNull]
	public string GetConfigString(string key, [CanBeNull] string defaultValue = null)
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
		ConfigFilePath = Path.Combine(dir, Constants.CONFIG_FOLDER);
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

	public void CreateIgnoreFile(Vcs system = Vcs.Git, bool forceCreate = false)
	{
		if (string.IsNullOrEmpty(ConfigFilePath))
			throw new CliException("No beamable project exists. Please use beam init");

		string ignoreFilePath = GetIgnoreFilePath(system);
		if (File.Exists(ignoreFilePath) && !forceCreate)
			return;

		var builder = new StringBuilder();
		foreach (var fileName in Constants.TEMP_FILES)
		{
			builder.Append('/');
			builder.Append(fileName);
			builder.Append(Environment.NewLine);
		}

		builder.Append(Constants.TEMP_FOLDER);
		builder.Append('/');
		builder.Append(Environment.NewLine);
		File.WriteAllText(ignoreFilePath, builder.ToString());

		BeamableLogger.Log($"Generated ignore file at {ignoreFilePath}");
	}

	public bool ReadTokenFromFile(out CliToken response)
	{
		response = null;
		string fullPath = Path.Combine(ConfigFilePath, Constants.TEMP_FOLDER, Constants.CONFIG_TOKEN_FILE_NAME);
		if (!File.Exists(fullPath)) return false;

		try
		{
			var content = File.ReadAllText(fullPath);
			response = JsonConvert.DeserializeObject<CliToken>(content);
			return true;
		}
		catch
		{
			// ignored
		}

		return false;
	}

	public void SaveTokenToFile(IAccessToken response)
	{
		string fullPath = Path.Combine(ConfigFilePath, Constants.TEMP_FOLDER, Constants.CONFIG_TOKEN_FILE_NAME);
		var json = JsonConvert.SerializeObject(response);
		File.WriteAllText(fullPath, json);
	}

	public void RemoveConfigFolderContent()
	{
		if (TryToFindBeamableConfigFolder(out var path))
		{
			var directory = new DirectoryInfo(path);
			foreach (FileInfo file in directory.GetFiles())
			{
				file.Delete();
			}

			foreach (DirectoryInfo subDirectory in directory.GetDirectories())
			{
				subDirectory.Delete(true);
			}
		}
	}

	void RefreshConfig()
	{
		ConfigFileExists = TryToFindBeamableConfigFolder(out var configPath);
		ConfigFilePath = configPath;

		if (!Directory.Exists(GetConfigPath(Constants.TEMP_FOLDER)))
		{
			Directory.CreateDirectory(GetConfigPath(Constants.TEMP_FOLDER));
		}
		
		MigrateOldConfigIfExists();
		var isValid = TryToReadConfigFile(ConfigFilePath, out _config);
		if (ConfigFileExists.Value && !isValid)
		{
			throw new CliException(
				$"Beamable Config exist but it does not have one of the values: {string.Join(',', Constants.REQUIRED_CONFIG_KEYS)}");
		}
	}

	/// <summary>
	/// This commands goes through RENAMED_FILES dictionary and looks for files in config with obsolete names and renames
	/// them to currently used names.
	/// </summary>
	private void MigrateOldConfigIfExists()
	{
		foreach (var renamedPair in Constants.RENAMED_FILES)
		{
			var oldPath = GetConfigPath(renamedPair.Key);
			if (File.Exists(oldPath))
			{
				File.Move(oldPath,GetConfigPath(renamedPair.Value));
			}
		}

		foreach (var pair in Constants.RENAMED_DIRECTORIES)
		{
			var oldPath = GetConfigPath(pair.Key);
			if (Directory.Exists(oldPath))
			{
				Directory.Move(oldPath,GetConfigPath(pair.Value));
			}
		}
	}

	bool TryToFindBeamableConfigFolder(out string result)
	{
		result = string.Empty;
		var basePath = _dir;
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

	bool TryToReadConfigFile([CanBeNull] string folderPath, out Dictionary<string, string> result)
	{
		string fullPath = Path.Combine(folderPath ?? string.Empty, Constants.CONFIG_DEFAULTS_FILE_NAME);
		result = new Dictionary<string, string>();
		if (!File.Exists(fullPath))
		{
			return false;
		}

		var content = File.ReadAllText(fullPath);
		result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

		return IsConfigValid(result);
	}

	string GetIgnoreFilePath(Vcs system) =>
		system switch
		{
			Vcs.Git => Path.Combine(ConfigFilePath, Constants.CONFIG_GIT_IGNORE_FILE_NAME),
			Vcs.SVN => Path.Combine(ConfigFilePath, Constants.CONFIG_SVN_IGNORE_FILE_NAME),
			Vcs.P4 => Path.Combine(ConfigFilePath, Constants.CONFIG_P4_IGNORE_FILE_NAME),
			_ => throw new ArgumentOutOfRangeException(nameof(system), system, $"VCS {system} is not supported")
		};

	static bool IsConfigValid(Dictionary<string, string> dict)
	{
		if (dict == null || dict.Count == 0) return false;

		return Constants.REQUIRED_CONFIG_KEYS.All(dict.Keys.Contains);
	}
}
