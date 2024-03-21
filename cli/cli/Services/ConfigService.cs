using Beamable.Common;
using Beamable.Common.Api;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine.Binding;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
	public bool? DirectoryExists { get; private set; }
	[CanBeNull] public string ConfigDirectoryPath { get; private set; }
	public string BaseDirectory => Path.GetDirectoryName(ConfigDirectoryPath);

	[CanBeNull] private Dictionary<string, string> _config;

	private string _dir;
	private string WorkingDirectoryFullPath => Path.GetFullPath(WorkingDirectory);

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
	/// Check if the given path is within the working directory.
	/// </summary>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is within the working directory, false otherwise.</returns>
	public bool IsPathInWorkingDirectory(string path)
	{
		var fullPath = Path.GetFullPath(path);
		return fullPath.StartsWith(WorkingDirectoryFullPath);
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

		var path = Path.Combine(WorkingDirectoryFullPath, relativePath);
		var baseDir = "";
		var relativeTo = Directory.GetCurrentDirectory();
		if (!string.IsNullOrEmpty(BaseDirectory))
		{
			baseDir = Path.GetRelativePath(WorkingDirectoryFullPath, BaseDirectory);
			relativeTo = Path.Combine(Directory.GetCurrentDirectory(), baseDir);
		}

		path = Path.GetRelativePath(relativeTo, path);
		Log.Verbose(
			$"Converting path=[{relativePath}] into .beamable relative path, result=[{path}], workingDir=[{Directory.GetCurrentDirectory()}] workingDirFull=[{WorkingDirectoryFullPath}] baseDir=[{baseDir}]");
		return path;
	}

	public string BeamableRelativeToExecutionRelative(string relativePath)
	{
		var path = GetFullPath(relativePath);
		var executionRelative = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
		Log.Verbose($"Converting path=[{relativePath}] into execution relative path, result=[{executionRelative}], base=[{BaseDirectory}] workingDir=[{Directory.GetCurrentDirectory()}] path=[{path}]");
		return executionRelative;
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
		if (string.IsNullOrWhiteSpace(ConfigDirectoryPath))
		{
			throw new CliException($"Could not find {pathInConfig} because config file is unspecified.");
		}
		var basePath = ConfigDirectoryPath;
		if (Constants.TEMP_FILES.Contains(pathInConfig))
		{
			basePath = Path.Combine(basePath, Constants.TEMP_FOLDER);
		}

		return Path.Combine(basePath, pathInConfig);
	}

	public void SaveDataFile<T>(string fileName, T data)
	{
		var json = JsonConvert.SerializeObject(data,
			new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		var file = GetConfigPath(fileName);
		File.WriteAllText(file, json);
	}

	public T LoadDataFile<T>(string fileName) where T : new() => LoadDataFile<T>(fileName, () => new T());

	public T LoadDataFile<T>(string fileName, Func<T> defaultValueGenerator)
	{
		try
		{
			if (!DirectoryExists.GetValueOrDefault(false))
			{
				return defaultValueGenerator();
			}
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
		ConfigDirectoryPath = Path.Combine(dir, Constants.CONFIG_FOLDER);
	}

	public void FlushConfig()
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");
		var json = JsonConvert.SerializeObject(_config,
			new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		if (!Directory.Exists(ConfigDirectoryPath))
		{
			Directory.CreateDirectory(ConfigDirectoryPath);
		}

		if (!Directory.Exists(GetConfigPath(Constants.TEMP_FOLDER)))
		{
			Directory.CreateDirectory(GetConfigPath(Constants.TEMP_FOLDER));
		}

		string fullPath = Path.Combine(ConfigDirectoryPath, Constants.CONFIG_DEFAULTS_FILE_NAME);
		File.WriteAllText(fullPath, json);
	}

	public void CreateIgnoreFile(Vcs system = Vcs.Git, bool forceCreate = false)
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
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
		string fullPath = Path.Combine(ConfigDirectoryPath, Constants.TEMP_FOLDER, Constants.CONFIG_TOKEN_FILE_NAME);
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
		string fullPath = Path.Combine(ConfigDirectoryPath, Constants.TEMP_FOLDER, Constants.CONFIG_TOKEN_FILE_NAME);
		var json = JsonConvert.SerializeObject(response,
			new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
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

	public void RefreshConfig()
	{
		DirectoryExists = TryToFindBeamableConfigFolder(out var configPath);
		ConfigDirectoryPath = configPath;
		if (DirectoryExists.GetValueOrDefault(false))
		{
			if (!Directory.Exists(GetConfigPath(Constants.TEMP_FOLDER)))
			{
				Directory.CreateDirectory(GetConfigPath(Constants.TEMP_FOLDER));
			}

			MigrateOldConfigIfExists();
		}
		TryToReadConfigFile(ConfigDirectoryPath, out _config);
	}

	/// <summary>
	/// This commands goes through RENAMED_FILES dictionary and looks for files in config with obsolete names and renames
	/// them to currently used names.
	/// </summary>
	private void MigrateOldConfigIfExists()
	{
		foreach (string key in Constants.RENAMED_DIRECTORIES.Keys)
		{
			var oldPath = GetConfigPath(key);
			if (Directory.Exists(oldPath))
			{
				var newPath = GetConfigPath(Constants.RENAMED_DIRECTORIES[key] as string);
				var existsAndAreDifferent = Directory.Exists(newPath);
				var samePath = string.Compare(oldPath, newPath,
					RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture) == 0;

				existsAndAreDifferent &= !samePath;

				if (existsAndAreDifferent)
				{
					throw new CliException($"Config resolution error, there is {oldPath} and {newPath}",
						Constants.CMD_RESULT_CONFIG_RESOLUTION_CONFLICT, true,
						"Remove one of the directories and run the command again\n");
				}

				if (samePath)
				{
					var tmpPath = newPath + "_bak";
					if (Directory.Exists(tmpPath))
					{
						throw new CliException($"Config resolution error, there is already temp dir {tmpPath}",
							Constants.CMD_RESULT_CONFIG_RESOLUTION_CONFLICT, true,
							"Remove dir and run command again\n");
					}
					Directory.Move(oldPath, tmpPath);
					oldPath = tmpPath;
				}
				Directory.Move(oldPath, newPath!);
			}
		}

		foreach (string key in Constants.RENAMED_FILES.Keys)
		{
			var oldPath = GetConfigPath(key);
			if (File.Exists(oldPath))
			{
				var newPath = GetConfigPath(Constants.RENAMED_FILES[key] as string);
				var existsAndAreDifferent = File.Exists(newPath);

				existsAndAreDifferent &=
						string.Compare(oldPath, newPath,
							RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture) != 0;
				existsAndAreDifferent &= !HaveSameContent(oldPath, newPath);

				if (existsAndAreDifferent)
				{
					throw new CliException($"Config resolution error, there is {oldPath} and {newPath}",
						Constants.CMD_RESULT_CONFIG_RESOLUTION_CONFLICT, true,
						"Remove one of the files and run the command again\n");
				}
				File.Move(oldPath, newPath!);
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
			Vcs.Git => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_GIT_IGNORE_FILE_NAME),
			Vcs.SVN => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_SVN_IGNORE_FILE_NAME),
			Vcs.P4 => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_P4_IGNORE_FILE_NAME),
			_ => throw new ArgumentOutOfRangeException(nameof(system), system, $"VCS {system} is not supported")
		};

	static bool IsConfigValid(Dictionary<string, string> dict)
	{
		if (dict == null || dict.Count == 0) return false;

		return Constants.REQUIRED_CONFIG_KEYS.All(dict.Keys.Contains);
	}

	static bool HaveSameContent(string pathFirst, string pathSecond)
	{
		using var md5 = MD5.Create();
		var first = md5.ComputeHash(File.ReadAllBytes(pathFirst));
		var second = md5.ComputeHash(File.ReadAllBytes(pathSecond));
		return first.SequenceEqual(second);
	}
}
