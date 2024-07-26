using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Serialization.SmallerJSON;
using cli.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine.Binding;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
	/// Check if the given path is within the .beamable folder context.
	/// </summary>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is within the .beamable folder context, false otherwise.</returns>
	public bool IsPathInBeamableDirectory(string path)
	{
		var fullPath = Path.GetFullPath(path);
		var parent = Path.GetDirectoryName(ConfigDirectoryPath);
		if (parent == "") parent = ".";
		return fullPath.StartsWith(Path.GetFullPath(parent));
	}

	/// <summary>
	/// Get the docker build context path, which is used to copy files through the Dockerfile
	/// </summary>
	/// <returns></returns>
	public string GetAbsoluteDockerBuildContextPath()
	{
		var path = BaseDirectory;
		if (string.IsNullOrEmpty(path))
		{
			path = _dir;
		}

		return Path.GetFullPath(path);
	}

	/// <summary>
	/// Gets a path relative to the docker build context path
	/// </summary>
	/// <param name="path">Your current path</param>
	/// <returns>A path relative to the docker build context path</returns>
	public string GetRelativeToDockerBuildContextPath(string path)
	{
		string absoluteContextPath = GetAbsoluteDockerBuildContextPath();
		return Path.GetRelativePath(absoluteContextPath, path);
	}

	public string GetPathFromRelativeToService(string path, string servicePath)
	{
		var relativePath = Path.GetDirectoryName(path);
		var fullPath = Path.Combine(servicePath, relativePath);
		return GetRelativeToDockerBuildContextPath(fullPath);
	}

	/// <summary>
	/// by default, paths are relative to the execution working directory...
	/// But you may need them to be relative to the project root.
	///
	/// This function will take a relative directory from the execution site, and turn it into a relative path from the project's root.
	/// The project's root is the folder that _contains_ /.beamable
	/// </summary>
	/// <param name="executionRelativePath"></param>
	/// <returns></returns>
	public string GetRelativeToBeamableFolderPath(string executionRelativePath)
	{
		try
		{
			var path = Path.Combine(WorkingDirectoryFullPath, executionRelativePath);
			var baseDir = "";
			var relativeTo = Directory.GetCurrentDirectory();
			if (!string.IsNullOrEmpty(BaseDirectory))
			{
				baseDir = Path.GetRelativePath(WorkingDirectoryFullPath, BaseDirectory);
				relativeTo = Path.Combine(Directory.GetCurrentDirectory(), baseDir);
			}

			path = Path.GetRelativePath(relativeTo, path);
			return path;
		}
		catch (Exception)
		{
			Log.Verbose(
				$"Converting path=[{executionRelativePath}] into .beamable relative path, workingDir=[{Directory.GetCurrentDirectory()}] workingDirFull=[{WorkingDirectoryFullPath}] baseDir=[{BaseDirectory}]");
			throw;
		}
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

	public const string ENV_VAR_WINDOWS_VOLUME_NAMES = "BEAM_DOCKER_WINDOWS_CONTAINERS";
	public const string ENV_VAR_DOCKER_URI = "BEAM_DOCKER_URI";
	public const string ENV_VAR_DOCKER_EXE = "BEAM_DOCKER_EXE";

	/// <summary>
	/// Enabling a custom Docker Uri allows for a customer to have a customized docker install and still
	/// tell the Beam CLI where the docker socket is available.
	/// </summary>
	public string CustomDockerUri => Environment.GetEnvironmentVariable(ENV_VAR_DOCKER_URI);

	/// <summary>
	/// Beamable CLI needs the path to the docker executable for buildkit invocation. By default, the Beam CLI
	/// will make a guess where Docker's exe is, but it can be specified and overwritten with this env var
	/// </summary>
	public static string CustomDockerExe => Environment.GetEnvironmentVariable(ENV_VAR_DOCKER_EXE);

	/// <summary>
	/// Github Action Runners for windows don't seem to work with volumes for mongo.
	/// </summary>
	public bool UseWindowsStyleVolumeNames
	{
		get
		{
			var value = Environment.GetEnvironmentVariable(ENV_VAR_WINDOWS_VOLUME_NAMES);
			return !string.IsNullOrEmpty(value) && value != "0" && !value.ToLowerInvariant().StartsWith("f");
		}
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

	/// <summary>
	/// Called to initialize or overwrite the current DotNet dotnet-tools.json file in the ".beamable" folder's sibling ".config" folder.  
	/// </summary>
	public void EnforceDotNetToolsManifest()
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		var pathToDotNetConfigFolder = Directory.GetParent(ConfigDirectoryPath).ToString();
		pathToDotNetConfigFolder = Path.Combine(pathToDotNetConfigFolder, ".config");

		// Create the sibling ".config" folder if its not there.
		if (!Directory.Exists(pathToDotNetConfigFolder))
			Directory.CreateDirectory(pathToDotNetConfigFolder);

		// Create/Update the manifest inside the ".config" folder 
		var pathToToolsManifest = Path.Combine(pathToDotNetConfigFolder, "dotnet-tools.json");
		string manifestString;

		var executingCliVersion = VersionService.GetNugetPackagesForExecutingCliVersion().ToString();

		// Create the file if it doesn't exist with our default local tool and its correct version.
		if (!File.Exists(pathToToolsManifest))
		{
			manifestString = $@"{{
  ""version"": 1,
  ""isRoot"": true,
  ""tools"": {{
    ""beamable.tools"": {{
      ""version"": ""{executingCliVersion}"",
      ""commands"": [
        ""beam""
      ]
    }}
  }}
}}";
		}
		// If the file is already there, make a best effort to update just the beamable version.
		else
		{
			var versionMatching = new Regex("beamable.*?\"([0-9]+\\.[0-9]+\\.[0-9]+)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			manifestString = File.ReadAllText(pathToToolsManifest);

			if (versionMatching.IsMatch(manifestString))
			{
				// Replace the group within the full match with version number of the executing CLI
				manifestString = versionMatching.Replace(manifestString, match =>
				{
					var fullMatch = match.Value;
					return fullMatch.Replace(match.Groups[1].Value, executingCliVersion);
				});
			}
			else
			{
				if (!Json.IsValidJson(manifestString))
					throw new CliException("DotNet tool manifest is not valid json. Please correct it or remove it and re-run `beam init` so we can regenerate it.");

				var manifest = (ArrayDict)Json.Deserialize(manifestString);

				if (!manifest.ContainsKey("tools"))
					throw new CliException("DotNet tool manifest is not valid json. Please correct it or remove it and re-run `beam init` so we can regenerate it.");

				// Prepare the correct value for the "beamable.tools" entry into the manifest file.
				var toolsDict = new ArrayDict();
				toolsDict.Add("version", executingCliVersion);
				toolsDict.Add("commands", new[] { "beam" });

				// Update the tools JSON object 
				var tools = (ArrayDict)manifest["tools"];
				tools["beamable.tools"] = toolsDict;

				// Serialize the manifest back
				manifestString = Json.Serialize(manifest, new StringBuilder());
			}
		}

		File.WriteAllText(pathToToolsManifest, manifestString);
	}


	/// <summary>
	/// Extract the CLI version registered in the ".config" directory sibling to the ".beamable" folder. 
	/// </summary>
	public bool TryGetProjectBeamableCLIVersion(out string version)
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath) || !Directory.Exists(Directory.GetParent(ConfigDirectoryPath)?.ToString()))
		{
			version = "";
			return false;
		}

		var pathToDotNetConfigFolder = Directory.GetParent(ConfigDirectoryPath).ToString();
		pathToDotNetConfigFolder = Path.Combine(pathToDotNetConfigFolder, ".config");
		var pathToToolsManifest = Path.Combine(pathToDotNetConfigFolder, "dotnet-tools.json");

		if (!File.Exists(pathToToolsManifest))
		{
			version = "";
			return false;
		}

		var versionMatching = new Regex("beamable.*?\"([0-9]+\\.[0-9]+\\.[0-9]+.*?)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
		var versionMatch = versionMatching.Match(File.ReadAllText(pathToToolsManifest));

		if (versionMatch.Success)
		{
			var retrievedVersion = versionMatch.Groups[1].Value;

			if (!PackageVersion.TryFromSemanticVersionString(retrievedVersion, out _))
			{
				throw new CliException("The version in the dotnet-tools.json file is not valid.");
			}

			version = retrievedVersion;
			return true;
		}

		throw new CliException("Missing \"beamable.tools\" entry in \".config/dotnet-tools.json\" directory.");
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

		Log.Debug($"Generated ignore file at {ignoreFilePath}");
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
		var dir = Path.GetDirectoryName(fullPath);
		Directory.CreateDirectory(dir);
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
			var newPath = GetConfigPath(Constants.RENAMED_DIRECTORIES[key] as string);

			// We skip converting if the new directory is there. This means Case-Sensitive Renames don't get modified on windows --- which is irrelevant.
			if (Directory.Exists(newPath))
				continue;

			// If the old path exists and the new path doesn't, we move the old path into the new path.
			if (Directory.Exists(oldPath))
				Directory.Move(oldPath, newPath!);
		}

		foreach (string key in Constants.RENAMED_FILES.Keys)
		{
			var oldPath = GetConfigPath(key);
			var newPath = GetConfigPath(Constants.RENAMED_FILES[key] as string);

			// We skip converting if the new directory is there. This means Case-Sensitive Renames don't get modified on windows --- which is irrelevant.
			if (File.Exists(newPath))
				continue;

			// If the old Path is there, we simply move it to the NewPath
			if (File.Exists(oldPath))
				File.Move(oldPath, newPath!);
		}
	}

	bool TryToFindBeamableConfigFolder(out string result) => TryToFindBeamableFolder(_dir, out result);

	public static bool TryToFindBeamableFolder(string relativePath, out string result)
	{
		result = string.Empty;
		var basePath = relativePath;
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
