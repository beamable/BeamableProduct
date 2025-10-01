using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Util;
using Beamable.Serialization.SmallerJSON;
using cli.Options;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.CommandLine.Binding;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Beamable.Server;
using Microsoft.Extensions.Logging;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace cli;

[Serializable]
public enum Vcs
{
	Git,

	// ReSharper disable once InconsistentNaming
	SVN,
	P4
}

[Serializable]
public class OtelConfig
{
	public bool BeamCliAllowTelemetry;
	public string BeamCliTelemetryLogLevel;
	public long BeamCliTelemetryMaxSize;
}



public class ConfigService
{
	private readonly CliEnvironment _environment;
	private readonly ConfigDirOption _configDirOption;

	/// <summary>
	/// 
	/// </summary>
	public string WorkingDirectory => _workingDirectory;

	/// <summary>
	/// Path to the folder containing the <see cref="ConfigDirectoryPath"/>.
	/// </summary>
	public string BaseDirectory => Path.GetDirectoryName(ConfigDirectoryPath);

	/// <summary>
	/// Whether we are operating inside a "Beamable" context or not. This is defined by whether we have a <see cref="Constants.CONFIG_FOLDER"/> in our parent directory chain.
	/// </summary>
	public bool? DirectoryExists { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigDirectoryPath { get; private set; }
	
	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>/<see cref="Constants.TEMP_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>/<see cref="Constants.TEMP_FOLDER"/>/<see cref="Constants.TEMP_OTEL_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>/<see cref="Constants.TEMP_FOLDER"/>/<see cref="Constants.TEMP_OTEL_FOLDER"/>/<see cref="Constants.TEMP_OTEL_TRACES_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelTracesDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>/<see cref="Constants.TEMP_FOLDER"/>/<see cref="Constants.TEMP_OTEL_FOLDER"/>/<see cref="Constants.TEMP_OTEL_LOGS_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelLogsDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_FOLDER"/>/<see cref="Constants.TEMP_FOLDER"/>/<see cref="Constants.TEMP_OTEL_FOLDER"/>/<see cref="Constants.TEMP_OTEL_METRICS_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelMetricsDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="Constants.CONFIG_LOCAL_OVERRIDES_DIRECTORY"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigLocalOverridesDirectoryPath { get; private set; }


	/// <summary>
	/// Data from the <see cref="Constants.CONFIG_DEFAULTS_FILE_NAME"/>.
	/// </summary>
	private Dictionary<string, string> _config;

	/// <summary>
	/// Data from the <see cref="Constants.CONFIG_DEFAULTS_FILE_NAME"/> that lives inside the <see cref="Constants.CONFIG_LOCAL_OVERRIDES_DIRECTORY"/>.
	/// </summary>
	private Dictionary<string, string> _configLocalOverrides;
	

	private string _workingDirectory;

	private BindingContext _bindingCtx;
	private string WorkingDirectoryFullPath => Path.GetFullPath(WorkingDirectory);

	public ConfigService(CliEnvironment environment, ConfigDirOption configDirOption, BindingContext bindingCtx)
	{
		_bindingCtx = bindingCtx;
		_environment = environment;
		_configDirOption = configDirOption;

		_config = new();
		_configLocalOverrides = new();
	}

	/// <summary>
	/// Changes the <see cref="_workingDirectory"/> and recomputes <see cref="DirectoryExists"/>, the <see cref="ConfigDirectoryPath"/> and <see cref="ConfigLocalOverridesDirectoryPath"/>
	/// based on the new <see cref="_workingDirectory"/>.
	/// 
	/// If a null/empty string is provided, this will use the <see cref="Directory.GetCurrentDirectory"/>.
	/// </summary>
	public void SetWorkingDir(string dir = null)
	{
		// If no directory is provided, assume the current directory is the working directory.
		if (string.IsNullOrEmpty(dir) && !TryGetSetting(out dir, _bindingCtx, _configDirOption))
		{
			dir = Directory.GetCurrentDirectory();
		}
		_workingDirectory = dir;

		// We try to find a ".beamable" folder going up the hierarchy from the working directory.
		DirectoryExists = TryToFindBeamableFolder(_workingDirectory, out var configPath);

		// If the directory exists in our hierarchy, let's set that as the config path.
		// Otherwise, we set it as whatever the working directory is.
		configPath = DirectoryExists.HasValue && DirectoryExists.Value ? configPath : _workingDirectory;

		// If the config path is not a ".beamable" folder, we append it.
		// This means that --- outside of a "Beamable Context", the config path is '_workingDirectory/.beamable'
		// (which means that if/when we flush the config, that's where the .beamable context will be created).
		// This is a useful property to have on this function due to how our initialization is set up. 
		if (!configPath.EndsWith(Constants.CONFIG_FOLDER))
			configPath = Path.Combine(configPath, Constants.CONFIG_FOLDER);

		// Compute the Config and Local Overrides Path based off the found configPath.
		ConfigDirectoryPath = configPath;
		ConfigTempDirectoryPath = Path.Combine(ConfigDirectoryPath, Constants.TEMP_FOLDER);
		ConfigLocalOverridesDirectoryPath = Path.Combine(ConfigDirectoryPath, Constants.CONFIG_LOCAL_OVERRIDES_DIRECTORY);
		ConfigTempOtelDirectoryPath = Path.Combine(ConfigTempDirectoryPath, Constants.TEMP_OTEL_FOLDER);
		ConfigTempOtelLogsDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, Constants.TEMP_OTEL_LOGS_FOLDER);
		ConfigTempOtelTracesDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, Constants.TEMP_OTEL_TRACES_FOLDER);
		ConfigTempOtelMetricsDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, Constants.TEMP_OTEL_METRICS_FOLDER);
	}

	/// <summary>
	/// Reloads the files at <see cref="ConfigDirectoryPath"/> and <see cref="ConfigLocalOverridesDirectoryPath"/> and parses them out while validating. 
	/// This is a no-op when called outside a valid beamable context (as in, if <see cref="DirectoryExists"/> is `false`).  
	/// </summary>
	public void RefreshConfig()
	{
		if (DirectoryExists.GetValueOrDefault(false))
		{
			if (!Directory.Exists(GetConfigPath(Constants.TEMP_FOLDER)))
			{
				Directory.CreateDirectory(GetConfigPath(Constants.TEMP_FOLDER));
			}

			MigrateOldConfigIfExists();

			_ = ReadConfigFile(ConfigDirectoryPath, false, true, out _config);
			_ = ReadConfigFile(ConfigLocalOverridesDirectoryPath, true, false, out _configLocalOverrides);
		}
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

	/// <summary>
	/// Check if the given path is within the .beamable folder context.
	/// </summary>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is within the .beamable folder context, false otherwise.</returns>
	public bool IsPathInBeamableDirectory(string path)
	{
		var fullPath = Path.GetFullPath(path);

		var parent = BaseDirectory;
		if (string.IsNullOrEmpty(parent))
			parent = ".";

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
			path = _workingDirectory;
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
		var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		var file = GetConfigPath(fileName);
		var dir = Path.GetDirectoryName(file);
		Directory.CreateDirectory(dir);

		File.WriteAllText(file, json);
	}

	public OtelConfig LoadOtelConfigFromFile()
	{
		var config = LoadDataFile<OtelConfig>(CONFIG_FILE_OTEL);

		if (string.IsNullOrEmpty(config.BeamCliTelemetryLogLevel))
		{
			config.BeamCliTelemetryLogLevel = nameof(LogLevel.Warning);
		}

		if (config.BeamCliTelemetryMaxSize == 0)
		{
			config.BeamCliTelemetryMaxSize = Otel.MAX_OTEL_TEMP_DIR_SIZE;
		}

		return config;
	}

	public void SaveOtelConfigToFile(OtelConfig config)
	{
		SaveDataFile<OtelConfig>(CONFIG_FILE_OTEL, config);
	}

	public bool ExistsOtelConfig()
	{
		return File.Exists(GetConfigPath(CONFIG_FILE_OTEL));
	} 

	public List<string> LoadExtraPathsFromFile() => LoadDataFile<List<string>>(CONFIG_FILE_EXTRA_PATHS);

	public List<string> LoadPathsToIgnoreFromFile()
	{
		var paths = LoadDataFile<List<string>>(CONFIG_FILE_PATHS_TO_IGNORE);
		return paths
			.Select(BeamableRelativeToExecutionRelative)
			.Select(Path.GetFullPath)
			.ToList();
	}

	public void SaveExtraPathsToFile(List<string> paths)
	{
		var currentPaths = LoadDataFile<List<string>>(CONFIG_FILE_EXTRA_PATHS);
		currentPaths.AddRange(paths);
		SaveDataFile<List<string>>(CONFIG_FILE_EXTRA_PATHS, currentPaths.Distinct().ToList());
	}
	public void SavePathsToIgnoreToFile(List<string> paths)
	{
		var currentPaths = LoadDataFile<List<string>>(CONFIG_FILE_PATHS_TO_IGNORE);
		currentPaths.AddRange(paths);
		SaveDataFile<List<string>>(CONFIG_FILE_PATHS_TO_IGNORE, currentPaths.Distinct().ToList());
	}

	public string GetProjectRootPath()
	{
		var relativePath = LoadDataFile<string>(CONFIG_FILE_PROJECT_PATH_ROOT, () => ".");
		var fullPath = Path.Combine(BaseDirectory, relativePath);
		return new DirectoryInfo(fullPath).FullName;
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

	public const string ENV_VAR_WINDOWS_VOLUME_NAMES = "BEAM_DOCKER_WINDOWS_CONTAINERS";
	public const string ENV_VAR_DOCKER_URI = "BEAM_DOCKER_URI";
	public const string ENV_VAR_BEAM_CLI_IS_REDIRECTED_COMMAND = "BEAM_CLI_IS_REDIRECTED_COMMAND";
	public const string ENV_VAR_DOCKER_EXE = "BEAM_DOCKER_EXE";

	public const string CONFIG_FILE_PROJECT_PATH_ROOT = "project-root-path.json";
	public const string CONFIG_FILE_EXTRA_PATHS = "additional-project-paths.json";
	public const string CONFIG_FILE_OTEL = "otel-config.json";
	public const string CONFIG_FILE_PATHS_TO_IGNORE = "project-paths-to-ignore.json";

	public static bool IsRedirected => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_BEAM_CLI_IS_REDIRECTED_COMMAND));

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

	public void GetProjectSearchPaths(out List<string> searchPaths)
	{
		searchPaths = new List<string>();
		var rootPath = GetProjectRootPath();
		var extraPaths = GetExtraProjectPaths();
		if (string.IsNullOrEmpty(rootPath))
		{
			rootPath = ".";
		}

		searchPaths.Add(rootPath);

		foreach (var extra in extraPaths)
		{
			if (string.IsNullOrEmpty(extra)) continue;

			if (Path.IsPathRooted(extra))
			{
				searchPaths.Add(extra);
			}
			else
			{
				var full = Path.Combine(rootPath, extra);
				searchPaths.Add(full);
			}
		}
	}

	/// <summary>
	/// 'extra project paths' tells the CLI to scan additional directories for .csproj files.
	/// These paths can be stored in a config file in the .beamable folder, or can be given
	/// on the CLI
	/// </summary>
	/// <param name="context"></param>
	/// <returns>A non-null list of paths</returns>
	public List<string> GetExtraProjectPaths()
	{
		var cliPaths = _bindingCtx.ParseResult.GetValueForOption(ExtraProjectPathOptions.Instance)?.ToList();
		var fileBasedPaths = LoadExtraPathsFromFile();

		var allPaths = new List<string>();
		allPaths.AddRange(fileBasedPaths);
		if (cliPaths != null)
		{
			allPaths.AddRange(cliPaths);
		}

		return allPaths;
	}


	public bool TryGetSetting(out string value, BindingContext context, ConfigurableOption option, string defaultValue = null)
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
		}

		var hasValue = !string.IsNullOrEmpty(value);
		return hasValue;
	}

	public string PrettyPrint() => JsonConvert.SerializeObject(_config, Formatting.Indented);

	[CanBeNull]
	public string GetConfigString(string key, [CanBeNull] string defaultValue = null)
	{
		if (_configLocalOverrides?.TryGetValue(key, out var value) ?? false)
		{
			return value;
		}

		return GetConfigStringIgnoreOverride(key, defaultValue);
	}

	
	[CanBeNull]
	public string GetConfigStringIgnoreOverride(string key, [CanBeNull] string defaultValue = null)
	{
		if (_config?.TryGetValue(key, out var value) ?? false)
		{
			return value;
		}

		return defaultValue;
	}

	/// <summary>
	/// Use this in conjunction with <see cref="FlushLocalOverrides"/> to flush a configuration setting that will NOT be version controlled AND takes precedence over the <see cref="Constants.CONFIG_DEFAULTS_FILE_NAME"/>.
	/// </summary>
	public string SetLocalOverride(string key, string value)
	{
		if (_configLocalOverrides != null) _configLocalOverrides[key] = value;
		return _configLocalOverrides[key];
	}

	/// <summary>
	/// Use this along with <see cref="FlushLocalOverrides"/> to remove a local override with the given key. 
	/// </summary>
	public bool DeleteLocalOverride(string key)
	{
		return _configLocalOverrides.Remove(key);
	}

	/// <summary>
	/// Use this along with <see cref="FlushConfig"/> to flush a configuration setting that WILL be Version Controlled AND is overriden by files in <see cref="Constants.CONFIG_LOCAL_OVERRIDES_DIRECTORY"/>.
	/// </summary>
	public string SetConfigString(string key, string value)
	{
		if (_config != null) _config[key] = value;
		return _config[key];
	}

	public void FlushConfig()
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		// Flush the config
		var json = JsonConvert.SerializeObject(_config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		if (!IsConfigValid(_config, true, out var missingRequiredKeys))
		{
			var err = "Config is not valid.";
			err += missingRequiredKeys.Count == 0 ? "" : $"\nMissing Keys: {string.Join(",", missingRequiredKeys)}";
			err += $"\nFull Json Below:\n{json}";
			throw new CliException(err);
		}

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
	/// Calling this function allows you to set the local config overrides.
	/// </summary>
	public void FlushLocalOverrides()
	{
		if (string.IsNullOrEmpty(ConfigLocalOverridesDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		// Flush the overrides
		var json = JsonConvert.SerializeObject(_configLocalOverrides, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		if (!IsConfigValid(_configLocalOverrides, false, out _))
			throw new CliException($"Config overrides are not valid. Overrides:\n{json}");

		if (!Directory.Exists(ConfigLocalOverridesDirectoryPath))
		{
			Directory.CreateDirectory(ConfigLocalOverridesDirectoryPath);
		}

		string fullPath = Path.Combine(ConfigLocalOverridesDirectoryPath, Constants.CONFIG_DEFAULTS_FILE_NAME);
		File.WriteAllText(fullPath, json);
	}
	
	/// <summary>
	/// Called to initialize or overwrite the current DotNet dotnet-tools.json file in the ".beamable" folder's sibling ".config" folder.  
	/// </summary>
	public void EnforceDotNetToolsManifest(out string pathToToolsManifest)
	{
		pathToToolsManifest = null;
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		var pathToDotNetConfigFolder = Directory.GetParent(ConfigDirectoryPath)!.ToString();
		pathToDotNetConfigFolder = Path.Combine(pathToDotNetConfigFolder, ".config");

		// Create the sibling ".config" folder if its not there.
		if (!Directory.Exists(pathToDotNetConfigFolder))
			Directory.CreateDirectory(pathToDotNetConfigFolder);

		// Create/Update the manifest inside the ".config" folder 
		pathToToolsManifest = Path.Combine(pathToDotNetConfigFolder, "dotnet-tools.json");
		string manifestString;

		var versionStr = BeamAssemblyVersionUtil.GetVersion<App>();
		// Create the file if it doesn't exist with our default local tool and its correct version.
		if (!File.Exists(pathToToolsManifest))
		{
			manifestString = $@"{{
  ""version"": 1,
  ""isRoot"": true,
  ""tools"": {{
    ""beamable.tools"": {{
      ""version"": ""{versionStr}"",
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
			var versionMatching = new Regex("beamable.*?\"([0-9]+\\.[0-9]+\\.[0-9]+.*?)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			manifestString = File.ReadAllText(pathToToolsManifest);

			if (versionMatching.IsMatch(manifestString))
			{
				// Replace the group within the full match with version number of the executing CLI
				manifestString = versionMatching.Replace(manifestString, match =>
				{
					var fullMatch = match.Value;
					return fullMatch.Replace(match.Groups[1].Value, versionStr);
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
				toolsDict.Add("version", versionStr);
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

	public bool TryGetProjectBeamableCLIVersion(out string version)
	{
		return TryGetProjectBeamableCLIVersion(ConfigDirectoryPath, out version);
	}

	/// <summary>
	/// Extract the CLI version registered in the ".config" directory sibling to the ".beamable" folder. 
	/// </summary>
	public static bool TryGetProjectBeamableCLIVersion(string configDirectoryPath, out string version)
	{
		version = "";
		if (string.IsNullOrEmpty(configDirectoryPath))
		{
			return false;
		}

		// Loop until we find the file OR we get to the disk root.
		string currentPath = configDirectoryPath;
		while (true)
		{
			// If we have no parents (reached the root dir), we simply step out.
			var parent = Path.GetDirectoryName(currentPath);
			if (string.IsNullOrEmpty(parent))
			{
				version = string.Empty;
				return false;
			}

			// If we found the path, hurray... other-wise, look one level up.
			var possibleConfigFilePath = Path.Combine(parent, ".config", "dotnet-tools.json");
			if (File.Exists(possibleConfigFilePath))
			{
				currentPath = possibleConfigFilePath;
				break;
			}

			currentPath = parent;
		}

		var pathToToolsManifest = currentPath;

		var versionMatching = new Regex("beamable.*?\"([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+.*?)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
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

		return false;
	}

	public void CreateIgnoreFile(Vcs system = Vcs.Git, bool forceCreate = false)
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		var ignoreFilePath = system switch
		{
			Vcs.Git => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_GIT_IGNORE_FILE_NAME),
			Vcs.SVN => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_SVN_IGNORE_FILE_NAME),
			Vcs.P4 => Path.Combine(ConfigDirectoryPath, Constants.CONFIG_P4_IGNORE_FILE_NAME),
			_ => throw new ArgumentOutOfRangeException(nameof(system), system, $"VCS {system} is not supported")
		};

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

		builder.Append(Constants.CONTENT_DIRECTORY);
		builder.Append('/');
		builder.Append(Environment.NewLine);
		
		File.WriteAllText(ignoreFilePath, builder.ToString());

		Log.Debug($"Generated ignore file at {ignoreFilePath}");
	}

	public bool ReadTokenFromFile(out CliToken response)
	{
		response = null;
		var fullPath = GetActiveTokenFilePath();
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
		string fullPath = GetActiveTokenFilePath();
		var dir = Path.GetDirectoryName(fullPath);
		Directory.CreateDirectory(dir!);
		var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		File.WriteAllText(fullPath, json);
	}

	public void DeleteTokenFile()
	{
		string fullPath = GetActiveTokenFilePath();
		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}
	}

	public string GetActiveTokenFilePath() => Path.Combine(ConfigDirectoryPath!, Constants.TEMP_FOLDER, Constants.CONFIG_TOKEN_FILE_NAME);

	public void RemoveConfigFolderContent()
	{
		if (TryToFindBeamableFolder(_workingDirectory, out var path))
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

	/// <summary>
	/// Utility function that goes up from the relative path looking for a folder with the name <see cref="Constants.CONFIG_FOLDER"/>.
	/// </summary>
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

	/// <summary>
	/// Takes in a folder path and tries to load
	/// </summary>
	/// <param name="folderPath"></param>
	/// <param name="result"></param>
	/// <exception cref="CliException"></exception>
	private static bool ReadConfigFile(string folderPath, bool isOptional, bool enforceRequiredFields, out Dictionary<string, string> result)
	{
		string fullPath = Path.Combine(folderPath, Constants.CONFIG_DEFAULTS_FILE_NAME);
		result = new Dictionary<string, string>();
		if (!File.Exists(fullPath))
		{
			if (isOptional)
			{
				return true;
			}

			BeamableLogger.LogWarning($"Config file was not found at {fullPath}!");
			return false;
		}

		var content = File.ReadAllText(fullPath);
		result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

		if (!IsConfigValid(result, enforceRequiredFields, out var missingRequiredKeys))
		{
			BeamableLogger.LogWarning($"Config file did not have the expected keys: {string.Join(",", missingRequiredKeys)}!");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Takes in a "Config" dictionary (mapped to <see cref="Constants.CONFIG_DEFAULTS_FILE_NAME"/> contents) and see if it has all the required keys for the CLI to function at all.
	/// </summary>
	public static bool IsConfigValid(Dictionary<string, string> dict, bool enforceRequiredFields, out List<string> missingRequiredKeys)
	{
		missingRequiredKeys = Constants.REQUIRED_CONFIG_KEYS.ToList();
		if (dict == null || (dict.Count == 0 && enforceRequiredFields))
			return false;

		missingRequiredKeys = missingRequiredKeys.Except(dict.Keys).ToList();
		return !enforceRequiredFields || missingRequiredKeys.Count == 0;
	}
}
