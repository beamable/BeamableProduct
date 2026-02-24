using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Util;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using cli.Commands.Project;
using cli.Options;
using cli.Unreal;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine.Binding;
using System.Diagnostics;
using System.Formats.Tar;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace cli;

public class ConfigService
{
	public const string ENV_VAR_WINDOWS_VOLUME_NAMES = "BEAM_DOCKER_WINDOWS_CONTAINERS";
	public const string ENV_VAR_DOCKER_URI = "BEAM_DOCKER_URI";
	public const string ENV_VAR_BEAM_CLI_IS_REDIRECTED_COMMAND = "BEAM_CLI_IS_REDIRECTED_COMMAND";
	public const string ENV_VAR_DOCKER_EXE = "BEAM_DOCKER_EXE";

	public const string CFG_FOLDER = ".beamable";

	public const string CFG_TOKEN_FILE_NAME = "auth.beam.json";
	public const string CFG_TOKEN_FILE_DIR = $"{TEMP_FOLDER_NAME}/{CFG_TOKEN_FILE_NAME}";
	public const string CFG_TOKEN_JSON_FIELD_ACCESS_TOKEN = "access-token";
	public const string CFG_TOKEN_JSON_FIELD_REFRESH_TOKEN = "refresh-token";

	public const string CFG_FILE_NAME = "config.beam.json";
	public const string CFG_JSON_FIELD_CLI_VERSION = "cliVersion";
	public const string CFG_JSON_FIELD_HOST = "host";
	public const string CFG_JSON_FIELD_CID = "cid";
	public const string CFG_JSON_FIELD_PID = "pid";
	public const string CFG_JSON_FIELD_PROJ_PATH_ROOT = "projectPathRoot";
	public const string CFG_JSON_FIELD_ARR_ADDITIONAL_PROJECT_PATHS = "additionalProjectPaths";
	public const string CFG_JSON_FIELD_ARR_IGNORED_PROJECT_PATHS = "ignoredProjectPaths";
	public const string CFG_JSON_FIELD_OBJ_OTEL = "otelConfig";
	public const string CFG_JSON_FIELD_OBJ_LINKED_ENGINE_PROJECTS = "linkedProjects";

	public const string SHARED_FOLDER_NAME = "shared";
	public const string LOCAL_FOLDER_NAME = "local";
	public const string TEMP_FOLDER_NAME = "temp";

	public const string CONTENT_FOLDER_NAME = "content";
	public const string CONTENT_DIR = $"{LOCAL_FOLDER_NAME}/{CONTENT_FOLDER_NAME}";

	public const string CONTENT_SNAPTSHOT_FOLDER_NAME = "contentSnapshots";
	public const string CONTENT_SNAPSHOTS_SHARED_DIR = $"{SHARED_FOLDER_NAME}/{CONTENT_SNAPTSHOT_FOLDER_NAME}";
	public const string CONTENT_SNAPSHOTS_LOCAL_DIR = $"{LOCAL_FOLDER_NAME}/{CONTENT_SNAPTSHOT_FOLDER_NAME}";

	public const string DEV_USER_FOLDER_NAME = "developerUser";
	public const string DEV_USER_SHARED_DIR = $"{SHARED_FOLDER_NAME}/{DEV_USER_FOLDER_NAME}";
	public const string DEV_USER_LOCAL_DIR = $"{LOCAL_FOLDER_NAME}/{DEV_USER_FOLDER_NAME}";

	public const string OTEL_FOLDER_NAME = "otel";
	public const string OTEL_LOGS_FOLDER_NAME = "logs";
	public const string OTEL_METRICS_FOLDER_NAME = "metrics";
	public const string OTEL_TRACES_FOLDER_NAME = "traces";
	public const string OTEL_DIR = $"{TEMP_FOLDER_NAME}/{OTEL_FOLDER_NAME}";

	/// <summary>
	/// Directory from which the CLI is running. 
	/// DON'T USE THIS unless you're working on CLI initialization things. Instead, prefer defining paths relative to the <see cref="BeamableWorkspace"/> and
	/// using <see cref="GetRelativeToExecutionPath"/> and <see cref="GetRelativeToBeamableWorkspacePath"/> to convert back and forth. 
	/// 
	/// This helps with the stability of serialized paths.
	/// </summary>
	public string WorkingDirectory => _workingDirectory;

	/// <summary>
	/// <inheritdoc cref="WorkingDirectory"/>
	/// </summary>
	private string _workingDirectory;

	/// <summary>
	/// Path to the folder containing the <see cref="ConfigDirectoryPath"/> (the folder that contains the /.beamable folder).
	/// </summary>
	public string BeamableWorkspace => Path.GetDirectoryName(ConfigDirectoryPath);

	/// <summary>
	/// Whether we are operating inside a "Beamable" context or not. This is defined by whether we have a <see cref="CFG_FOLDER"/> in our parent directory chain.
	/// </summary>
	public bool? DirectoryExists { get; private set; }

	/// <summary>
	/// The Version Control System currently managing the Beamable Workspace.
	/// </summary>
	public Vcs BeamableWorkspaceVcs { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>/<see cref="TEMP_FOLDER_NAME"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>/<see cref="TEMP_FOLDER_NAME"/>/<see cref="OTEL_FOLDER_NAME"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>/<see cref="TEMP_FOLDER_NAME"/>/<see cref="OTEL_FOLDER_NAME"/>/<see cref="OTEL_TRACES_FOLDER_NAME"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelTracesDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>/<see cref="TEMP_FOLDER_NAME"/>/<see cref="OTEL_FOLDER_NAME"/>/<see cref="OTEL_LOGS_FOLDER_NAME"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelLogsDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the <see cref="CFG_FOLDER"/>/<see cref="TEMP_FOLDER_NAME"/>/<see cref="OTEL_FOLDER_NAME"/>/<see cref="OTEL_METRICS_FOLDER_NAME"/>.
	/// </summary>
	[CanBeNull]
	public string ConfigTempOtelMetricsDirectoryPath { get; private set; }

	/// <summary>
	/// When <see cref="DirectoryExists"/>, this holds the path to the local overrides file.
	/// </summary>
	[CanBeNull]
	public string ConfigLocalDirectoryPath { get; private set; }


	private BindingContext _bindingCtx;

	public ConfigService(BindingContext bindingCtx)
	{
		_bindingCtx = bindingCtx;

	}

	public static bool IsRedirected => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_BEAM_CLI_IS_REDIRECTED_COMMAND));

	#region Initialization & Migrations

	/// <summary>
	/// Reloads the files at <see cref="ConfigDirectoryPath"/> and <see cref="ConfigLocalDirectoryPath"/> and parses them out while validating. 
	/// This is a no-op when called outside a valid beamable context (as in, if <see cref="DirectoryExists"/> is `false`).  
	/// </summary>
	public void RefreshConfig()
	{
		if (DirectoryExists.GetValueOrDefault(false))
		{
			if (!Directory.Exists(GetConfigPath(TEMP_FOLDER_NAME)))
			{
				Directory.CreateDirectory(GetConfigPath(TEMP_FOLDER_NAME));
			}

			RunMigrations();

		}
	}

	/// <summary>
	/// <inheritdoc cref="RunMigrations"/>
	/// </summary>
	private readonly struct Migration
	{
		/// <summary>
		/// Created with with <see cref="PackageVersion.TryFromSemanticVersionString"/>, this defines in which version this migration was added.
		/// Only versions older than this one will run this. 
		/// </summary>
		public readonly PackageVersion ForVersion;

		/// <summary>
		/// The function that runs to convert from the previous version to this one.
		/// </summary>
		public readonly Function MigrationFunction;

		public delegate void Function(string configPath, JObject configObject);

		public Migration(PackageVersion forVersion, Function migrationFunction)
		{
			ForVersion = forVersion;
			MigrationFunction = migrationFunction;
		}
	}

	/// <summary>
	/// This is our migration system. It runs BEFORE the config file is loaded into this system, but after
	/// the various <see cref="ConfigDirectoryPath"/> have been computed.
	///
	/// If the project is older than version 7.0, we need to modify the folder structure of the .beamable folder to make it compatible with
	/// the new migration system. Otherwise, we run the new migration flow.
	///
	/// The "config.beam.json" file keeps track of the last version of the CLI that ran a migration.
	/// When a CLI command runs in a Beamable Workspace, it compares the CLI version in that file with the list of migrations in its own version.
	/// It then sequentially executes every registered migration that has a version HIGHER than the stored version.
	/// Finally, it'll update the CLI version in the file to match the current running version.
	///
	/// To make this play nicely with our dev.sh flow, the 0.0.123 migration runs whenever the version number changes.
	/// To test the migration logic in the 0.0.123 migration when running from rider without having to re-run dev.sh:
	///   - Copy the .beamable folder out to some temporary location (recommended: BeamableWorkspace/Backup).
	///   - Repeat this until done:
	///     - Write the migration logic you want.
	///     - Run any command through Rider.
	///     - Restore the backed up .beamable folder.
	///   - Move the working migration logic to a numbered migration for the version in which the migration is shipping. 
	/// </summary>
	private void RunMigrations()
	{
		var newConfigFile = GetConfigPath(CFG_FILE_NAME);
		if (File.Exists(newConfigFile))
		{
			var existingContent = LockedRead(newConfigFile!);
			
			var existingConfig = JsonConvert.DeserializeObject<JObject>(existingContent);

			var existingConfigVersion = GetConfig(CFG_JSON_FIELD_CLI_VERSION, "0.0.123", existingConfig);
			var existingPackageVersion = PackageVersion.FromSemanticVersionString(existingConfigVersion);

			// Add the CLI version to the object
			if (TryGetProjectBeamableCLIVersion(out var version))
			{
				if (existingConfigVersion != version)
				{
					// Iterate over list of migrations and sequentially run each migration whose version number is ABOVE the current one.
					var sortedMigrations = GetMigrations().ToList();
					sortedMigrations.Sort((m1, m2) => m1.ForVersion < m2.ForVersion ? -1 : 1);
					foreach (var m in sortedMigrations)
					{
						if (m.ForVersion > existingPackageVersion || m.ForVersion.IsLocalDev)
						{
							m.MigrationFunction(newConfigFile, existingConfig);
						}
					}
				}
			}
		}
		// Migration from legacy config folder to the new config folder structure that supports the new migration system.
		else
		{
			var backup = CreateInMemoryTarball(ConfigDirectoryPath);

			try
			{
				// Rename the old config-defaults file.
				var oldConfigFile = GetConfigPath("connection-configuration.json");
				if (File.Exists(oldConfigFile)) File.Move(oldConfigFile, newConfigFile!);
				else return;

				var newConfig = JsonConvert.DeserializeObject<JObject>(LockedRead(newConfigFile!));

				// Check for additional projects file and ignored directory files and move their data over.
				{
					var additionalProjectPathsPath = GetConfigPath("additional-project-paths.json");
					if (File.Exists(additionalProjectPathsPath))
					{
						var fileText = File.ReadAllText(additionalProjectPathsPath);
						var fileData = JsonConvert.DeserializeObject<string[]>(fileText);
						SetConfig(CFG_JSON_FIELD_ARR_ADDITIONAL_PROJECT_PATHS, fileData, newConfig);
					}

					var pathsToIgnorePath = GetConfigPath("project-paths-to-ignore.json");
					if (File.Exists(pathsToIgnorePath))
					{
						var fileText = File.ReadAllText(pathsToIgnorePath);
						var fileData = JsonConvert.DeserializeObject<string[]>(fileText);
						SetConfig(CFG_JSON_FIELD_ARR_IGNORED_PROJECT_PATHS, fileData, newConfig);
					}

					var projPathRootPath = GetConfigPath("project-root-path.json");
					if (File.Exists(projPathRootPath))
					{
						var fileText = File.ReadAllText(projPathRootPath);
						var fileData = JsonConvert.DeserializeObject<string[]>(fileText);
						SetConfig(CFG_JSON_FIELD_PROJ_PATH_ROOT, fileData, newConfig);
					}
				}

				// Check for linked projects file and move it over while adjusting the Unity array to be an array of objects.
				{
					var linkedProjectsPath = GetConfigPath("linked-projects.json");
					if (File.Exists(linkedProjectsPath))
					{
						var fileText = File.ReadAllText(linkedProjectsPath);
						var fileData = JsonConvert.DeserializeObject<JObject>(fileText);

						var engineData = new EngineProjectData() { unityProjectsPaths = new(), unrealProjectsPaths = new(), };
						if (fileData.TryGetValue("unityProjectPaths", out var unityData))
						{
							if (unityData is JArray arr)
							{
								foreach (JToken j in arr)
									engineData.unityProjectsPaths.Add(new EngineProjectData.Unity() { Path = j.Value<string>() });
							}
						}

						if (fileData.TryGetValue("unrealProjectsPaths", out var ueData))
						{
							if (ueData is JArray arr)
							{
								for (int i = 0; i < arr.Count; i++)
								{
									var j = (JObject)arr[i];
									var projectData = JsonConvert.DeserializeObject<EngineProjectData.Unreal>(j.ToString());
									engineData.unrealProjectsPaths.Add(projectData);
								}
							}
						}

						SetConfig(CFG_JSON_FIELD_OBJ_LINKED_ENGINE_PROJECTS, engineData, newConfig);
					}
				}

				// Check for the Otel Config
				{
					var otelConfigPath = GetConfigPath("otel-config.json");
					if (File.Exists(otelConfigPath))
					{
						var fileText = File.ReadAllText(otelConfigPath);
						var fileData = JsonConvert.DeserializeObject<OtelConfig>(fileText);
						SetConfig(CFG_JSON_FIELD_OBJ_OTEL, fileData, newConfig);
					}
				}

				// Move files around 
				{
					// Dev User
					{
						var oldPath = GetConfigPath("developerUser");
						var newPath = GetConfigPath(DEV_USER_SHARED_DIR);
						if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
						{
							var parentDir = Path.GetDirectoryName(newPath!);
							if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir!);
							Directory.Move(oldPath, newPath!);
						}
					}

					// Content
					{
						var oldPath = GetConfigPath("content");
						var newPath = GetConfigPath(CONTENT_DIR);
						if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
						{
							var parentDir = Path.GetDirectoryName(newPath!);
							if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir!);
							Directory.Move(oldPath, newPath!);
						}
					}

					// Content Snapshot
					{
						var oldPath = GetConfigPath("content-snapshots");
						var newPath = GetConfigPath(CONTENT_SNAPSHOTS_SHARED_DIR);
						if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
						{
							var parentDir = Path.GetDirectoryName(newPath!);
							if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir!);
							Directory.Move(oldPath, newPath!);
						}
					}
				}

				// Delete all the old files and the old temp directory.
				{
					if (File.Exists(GetConfigPath("project-root-path.json"))) File.Delete(GetConfigPath("project-root-path.json"));
					if (File.Exists(GetConfigPath("linked-projects.json"))) File.Delete(GetConfigPath("linked-projects.json"));
					if (File.Exists(GetConfigPath("otel-config.json"))) File.Delete(GetConfigPath("otel-config.json"));
					if (File.Exists(GetConfigPath("project-paths-to-ignore.json"))) File.Delete(GetConfigPath("project-paths-to-ignore.json"));
					if (File.Exists(GetConfigPath("additional-project-paths.json"))) File.Delete(GetConfigPath("additional-project-paths.json"));

					Directory.Delete(GetConfigPath("temp"), true);

					FlushConfig(newConfig!, Path.GetDirectoryName(newConfigFile), true);
					
					CreateIgnoreFile(forceCreate: true);
				}
			}
			catch (Exception)
			{
				ReplaceDirectoryWithTarball(ConfigDirectoryPath, backup);
				throw;
			}
			finally
			{
				backup.Dispose();
			}
		}
	}

	/// <summary>
	/// List of migrations functions we run when updating versions between one another used by our <see cref="RunMigrations"/> function.
	///
	/// All migrations must follow the following rules:
	///   - Be idempotent.
	///   - Be resilient (see pattern with <see cref="CreateInMemoryTarball"/> and <see cref="ReplaceDirectoryWithTarball"/> in <see cref="RunMigrations"/> for more info).
	///   - Any old constant values should be string-literals inside the migration function (see pattern in <see cref="RunMigrations"/> legacy conversion).	
	///
	/// These rules are here to make sure we can look through this function and read, top-to-bottom, the history of the .beamable folder structure and data structures.
	/// </summary>
	private IEnumerable<Migration> GetMigrations()
	{
		/*
			var mig_1_1 = new Migration(PackageVersion.FromSemanticVersionString("7.1"), (configFilePath, config) =>
			{
				// ... do something with config to make it correct.

				// Update the version in the config file and save out the migrated file.
				FlushConfig(ref config!, configFilePath, true);
			}),
		*/

		return new List<Migration>()
		{
			// This means NEXT version.
			// Don't commit code in here. This is a sandbox function for us to be able to test migration logic in our 
			// day-to-day dev workflows.
			// 
			// To make this play nicely with our dev.sh script auto-increment build number, this migration logic 
			// always runs when the build number changes.
			// 
			// Once the migration logic is done and tested, copy this migration into a variable above, replace the version number
			// with the version number for which this migration logic should run and add it to this list.
			// Ie.:
			//   - If you're working on a migration for version 7.1, you start by writing code here.
			//   - Then, once its done and tested, copy this block of code and change the semantic version string for the copy to 7.1.
			//   - Delete your 7.1 code from this original block of code.
			//
			// When running commands directly from Rider, you can simply modify the cliVersion in the config.beam.json file between each run to be a number different then your current build number.
			// Ie.: If your curr build number is 0.0.123.10, if the cliVersion is 0.0.123.9, this code will run.
			new Migration(PackageVersion.FromSemanticVersionString("0.0.123"), (configPath, config) =>
			{
				// TODO: Write any code you want here to test migration logic for the next version.
			})
		};
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

	#endregion

	#region Path Management & Conversion

	/// <summary>
	/// Changes the <see cref="_workingDirectory"/> and recomputes <see cref="DirectoryExists"/>, the <see cref="ConfigDirectoryPath"/> and <see cref="ConfigLocalDirectoryPath"/>
	/// based on the new <see cref="_workingDirectory"/>.
	/// 
	/// If a null/empty string is provided, this will use the <see cref="Directory.GetCurrentDirectory"/>.
	/// </summary>
	public void SetWorkingDir(string dir = null)
	{
		// If no directory is provided, assume the current directory is the working directory.
		if (string.IsNullOrEmpty(dir))
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
		if (!configPath.EndsWith(CFG_FOLDER))
			configPath = Path.Combine(configPath, CFG_FOLDER);

		// Compute the Config and Local Overrides Path based off the found configPath.
		ConfigDirectoryPath = configPath;
		ConfigTempDirectoryPath = Path.Combine(ConfigDirectoryPath, TEMP_FOLDER_NAME);
		ConfigLocalDirectoryPath = Path.Combine(ConfigDirectoryPath, LOCAL_FOLDER_NAME);
		ConfigTempOtelDirectoryPath = Path.Combine(ConfigTempDirectoryPath, OTEL_FOLDER_NAME);
		ConfigTempOtelLogsDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, OTEL_LOGS_FOLDER_NAME);
		ConfigTempOtelTracesDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, OTEL_TRACES_FOLDER_NAME);
		ConfigTempOtelMetricsDirectoryPath = Path.Combine(ConfigTempOtelDirectoryPath, OTEL_METRICS_FOLDER_NAME);

		// Keep track of the current VCS managing this beamable workspace
		BeamableWorkspaceVcs = GetVcsType(ConfigDirectoryPath);
	}

	/// <summary>
	/// Check if the given path is within the <see cref="BeamableWorkspace"/>.
	/// </summary>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is within the <see cref="BeamableWorkspace"/>, false otherwise.</returns>
	public bool IsInBeamableWorkspace(string path)
	{
		var fullPath = Path.GetFullPath(path);

		var parent = BeamableWorkspace;
		if (string.IsNullOrEmpty(parent))
			parent = ".";

		return fullPath.StartsWith(Path.GetFullPath(parent));
	}

	/// <summary>
	/// By default, paths are relative to the execution working directory...
	/// But you may need them to be relative to the project root.
	///
	/// This function will take a relative directory from the execution site, and turn it into a relative path from the <see cref="BeamableWorkspace"/> 's root.	
	/// </summary>
	public string GetRelativeToBeamableWorkspacePath(string executionRelativePath)
	{
		var workingDirectoryFullPath = Path.GetFullPath(WorkingDirectory);
		try
		{
			var path = Path.Combine(workingDirectoryFullPath, executionRelativePath);
			var baseDir = "";
			var relativeTo = Directory.GetCurrentDirectory();
			if (!string.IsNullOrEmpty(BeamableWorkspace))
			{
				baseDir = Path.GetRelativePath(workingDirectoryFullPath, BeamableWorkspace);
				relativeTo = Path.Combine(Directory.GetCurrentDirectory(), baseDir);
			}

			path = Path.GetRelativePath(relativeTo, path);
			return path;
		}
		catch (Exception)
		{
			Log.Trace(
				$"Converting path=[{executionRelativePath}] into .beamable relative path, workingDir=[{Directory.GetCurrentDirectory()}] workingDirFull=[{workingDirectoryFullPath}] baseDir=[{BeamableWorkspace}]");
			throw;
		}
	}

	/// <summary>
	/// Sometimes you have a path store relative to the <see cref="BeamableWorkspace"/> but you need it relative to the execution context of the running CLI command.
	/// This function converts it.
	/// </summary>	
	public string GetRelativeToExecutionPath(string workspaceRelativePath)
	{
		var path = Path.Combine(BeamableWorkspace, workspaceRelativePath);
		var executionRelative = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
		Log.Trace($"Converting path=[{workspaceRelativePath}] into execution relative path, result=[{executionRelative}], base=[{BeamableWorkspace}] workingDir=[{Directory.GetCurrentDirectory()}] path=[{path}]");
		return executionRelative;
	}

	/// <summary>
	/// Gets the full path for a given configuration file or directory.
	/// </summary>
	/// <param name="relativePath">The relative path of the file or directory in the <see cref="ConfigDirectoryPath"/>. Use one of the various constants in this file (see <see cref="DEV_USER_LOCAL_DIR"/>).</param>
	/// <returns>The full path of the file in the configuration.</returns>
	public string GetConfigPath(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(ConfigDirectoryPath)) throw new CliException($"Could not find {relativePath} because config directory is unspecified. You should never see this.");
		return Path.Combine(ConfigDirectoryPath, relativePath);
	}

	#endregion

	#region Config JSON Management

	public bool TryGetSetting(out string value, BindingContext context, ConfigurableOption option, string defaultValue = null)
	{
		// Try to get from option and, if we can't, get it from the loaded config file.		
		value = context.ParseResult.GetValueForOption(option) ?? GetConfigString2(option.OptionName, defaultValue);
		return !string.IsNullOrEmpty(value);
	}

	public string PrettyPrint()
	{
		if (ReadConfigFile(ConfigDirectoryPath, true, false, out var config))
		{
			return JsonConvert.SerializeObject(config, Formatting.Indented);
		}

		return "";
	} 

	// [CanBeNull]
	// public string GetConfigString(string key, [CanBeNull] string defaultValue = null) =>
	// 	GetConfig<string>(key, defaultValue);
	//
	public string GetConfigString2(string key, [CanBeNull] string defaultValue = null) =>
		GetConfig2<string>(key, defaultValue);

	[CanBeNull]
	public string GetConfigStringIgnoreOverride(string key, [CanBeNull] string defaultValue = null) => GetConfig2<string>(key, defaultValue, true);

	public T GetConfig2<T>(string key, [CanBeNull] T defaultValue, bool ignoreOverride = false)
	{
		if (!ignoreOverride)
		{
			if (ReadConfigFile(ConfigLocalDirectoryPath, true, false, out var localConfig))
			{
				if (TryGetConfig(key, defaultValue, localConfig, out var value))
				{
					return value;
				}
			}
		}
		
		if (!ReadConfigFile(ConfigDirectoryPath, true, false, out var config))
		{
			return defaultValue;
		}

		TryGetConfig(key, defaultValue, config, out var currentValue);
		return currentValue; 
	}
	// public T GetConfig<T>(string key, [CanBeNull] T defaultValue, bool ignoreOverride = false)
	// {
	// 	var value = _configLocalOverrides?.SelectToken(key);
	// 	if (value != null)
	// 	{
	// 		if (!ignoreOverride)
	// 		{
	// 			return value switch
	// 			{
	// 				JObject json => JsonConvert.DeserializeObject<T>(json.ToString()),
	// 				JArray arr => JsonConvert.DeserializeObject<T>(arr.ToString()),
	// 				_ => value.Value<T>()
	// 			};
	// 		}
	// 	}
	//
	// 	return GetConfig(key, defaultValue, _config);
	// }

	public T GetConfig<T>(string key, [CanBeNull] T defaultValue, JObject config)
	{
		lock (_configLock)
		{
			var value = config?.SelectToken(key);
			if (value != null)
			{
				return value switch
				{
					JObject json => JsonConvert.DeserializeObject<T>(json.ToString()),
					JArray arr => JsonConvert.DeserializeObject<T>(arr.ToString()),
					_ => value.Value<T>()
				};
			}
		}

		return defaultValue;
	}
	
	
	public bool TryGetConfig<T>(string key, [CanBeNull] T defaultValue, JObject config, out T currentValue)
	{
		lock (_configLock)
		{
			var value = config?.SelectToken(key);
			if (value == null)
			{
				currentValue = defaultValue;
				return false;
			}
			
			currentValue = value switch
			{
				JObject json => JsonConvert.DeserializeObject<T>(json.ToString()),
				JArray arr => JsonConvert.DeserializeObject<T>(arr.ToString()),
				_ => value.Value<T>()
			};
			return true;
		}
	}

	public void WriteConfig(Action<JObject> modifier, bool isOverride = false)
	{
		var configFolder = isOverride
			? ConfigLocalDirectoryPath
			: ConfigDirectoryPath;

		ReadConfigFile(configFolder, true, false, out var config);
		var original = (JObject)config.DeepClone();
		modifier(config);

		var patch = DiffJObject(original, config);
		
		ReadConfigFile(configFolder, true, false, out var latestConfig);
		ApplyDiff(latestConfig, patch);
		FlushConfig(latestConfig, configFolder, !isOverride);
		
		
		// chat-gippity wrote these methods...
		JObject DiffJObject(JObject original, JObject modified)
		{
			var set = new JObject();
			var remove = new JArray();
			var children = new JObject();

			// Detect removed and changed properties
			foreach (var prop in original.Properties())
			{
				var name = prop.Name;

				if (!modified.TryGetValue(name, out var newValue))
				{
					remove.Add(name);
					continue;
				}

				var oldValue = prop.Value;

				if (oldValue.Type == JTokenType.Object &&
				    newValue.Type == JTokenType.Object)
				{
					var childDiff = DiffJObject((JObject)oldValue, (JObject)newValue);
					if (childDiff.HasValues)
					{
						children[name] = childDiff;
					}
				}
				else if (!JToken.DeepEquals(oldValue, newValue))
				{
					set[name] = newValue.DeepClone();
				}
			}

			// Detect added properties
			foreach (var prop in modified.Properties())
			{
				if (!original.ContainsKey(prop.Name))
				{
					set[prop.Name] = prop.Value.DeepClone();
				}
			}

			var diff = new JObject();

			if (set.HasValues) diff["$set"] = set;
			if (remove.HasValues) diff["$remove"] = remove;
			if (children.HasValues) diff["$children"] = children;

			return diff;
		}
		void ApplyDiff(JObject target, JObject diff)
		{
			if (diff == null || !diff.HasValues)
				return;

			// Apply removals
			if (diff["$remove"] is JArray removeArray)
			{
				foreach (var item in removeArray)
				{
					target.Remove(item.ToString());
				}
			}

			// Apply sets
			if (diff["$set"] is JObject setObj)
			{
				foreach (var prop in setObj.Properties())
				{
					target[prop.Name] = prop.Value.DeepClone();
				}
			}

			// Apply children recursively
			if (diff["$children"] is JObject childrenObj)
			{
				foreach (var childProp in childrenObj.Properties())
				{
					var childName = childProp.Name;
					var childDiff = (JObject)childProp.Value;

					if (target[childName] is JObject childTarget)
					{
						ApplyDiff(childTarget, childDiff);
					}
					else
					{
						// If missing or not an object, create a new object
						var newChild = new JObject();
						ApplyDiff(newChild, childDiff);
						target[childName] = newChild;
					}
				}
			}
		}

		
	}
	public T WriteConfig<T>(string path, [CanBeNull] T newValue, bool isOverride=false)
	{
		T nextValue = default;
		WriteConfig(config =>
		{
			nextValue = SetConfig(path, newValue, config);
		}, isOverride);
		return nextValue;
	}

	// public T SetConfig<T>(string path, [CanBeNull] T newValue, bool isOverride = false) => SetConfig(path, newValue, isOverride ? _configLocalOverrides : _config);

	public static T SetConfig<T>(string path, [CanBeNull] T newValue, JObject target)
	{
		lock (_configLock)
		{
			AddPropertyAtSubPath(target, path, newValue);
			return newValue;
		}

		static void AddPropertyAtSubPath(JObject json, string path, object value)
		{
			// Use SelectToken to find the parent JContainer using Newtonsoft's built-in path logic
			// We use the JSON path syntax which supports both dot and bracket notation.
			int lastDotIndex = path.LastIndexOf('.');
			int lastBracketIndex = path.LastIndexOf(']');

			string parentPath;
			string propertyName;

			if (lastBracketIndex > lastDotIndex) // Path ends with an array indexer e.g., "list[1]"
			{
				// We cannot add a *new* property name in this scenario, 
				// as this path segment is trying to *index* into an array or object. 
				// This function is for *adding* a property to an object/array.
				throw new ArgumentException("Cannot add a new property with a path ending in an array indexer (e.g., 'list[1]'). The path must specify the new property name/index.");
			}

			if (lastDotIndex != -1) // Path ends with a property name e.g., "object.newProp"
			{
				parentPath = path[..lastDotIndex];
				propertyName = path[(lastDotIndex + 1)..];
			}
			else // Path is just a single property name e.g., "newProp"
			{
				parentPath = null;
				propertyName = path;
			}

			var parent = string.IsNullOrEmpty(parentPath) ? json : CreateNestedJContainer(json, parentPath);

			// Add the new property to the parent container
			var tokenValue = JToken.FromObject(value);

			// If the parent is an array, we must use the Add method to append the new value
			if (parent is JObject parentObject) parentObject[propertyName] = tokenValue;
			else if (parent is JArray parentArray) parentArray.Add(tokenValue);
		}

		// This private helper ensures all intermediate containers (objects or arrays) exist
		static JContainer CreateNestedJContainer(JObject root, string path)
		{
			// Regex to match property names (dot notation) OR array indexers (bracket notation)
			var pathSegmentRegex = new Regex(@"([^\.\[\]]+)|\[(\d+)\]");

			JContainer current = root;
			// The regex helps split the path correctly, distinguishing between "prop" and "[index]"
			MatchCollection matches = pathSegmentRegex.Matches(path);

			foreach (Match match in matches)
			{
				// Group 1 captures property names, Group 2 captures array indexes
				string propertyName = match.Groups[1].Value;
				string indexValue = match.Groups[2].Value;

				// It's a property name segment
				if (!string.IsNullOrEmpty(propertyName))
				{
					if (current is JObject currentObj)
					{
						JToken token = currentObj[propertyName];
						if (token is JContainer existingContainer)
						{
							current = existingContainer;
						}
						else
						{
							var newObj = new JObject();
							currentObj[propertyName] = newObj;
							current = newObj;
						}
					}
					else
					{
						throw new InvalidOperationException($"Cannot access property '{propertyName}' on a non-object JContainer (path: {current.Path}).");
					}
				}
				// It's an array indexer segment
				else if (!string.IsNullOrEmpty(indexValue))
				{
					if (current is JArray currentArray)
					{
						int index = int.Parse(indexValue);
						if (index >= currentArray.Count)
						{
							// Handle cases where the index is out of bounds by creating empty objects/arrays as needed to reach the index
							while (index >= currentArray.Count)
							{
								// We don't know if the next element should be an object or an array based on path alone.
								// For simplicity, we add a null placeholder. User might need to refine logic here.
								currentArray.Add(JValue.CreateNull());
							}

							current = (JContainer)currentArray[index];
						}
						else
						{
							current = (JContainer)currentArray[index];
						}
					}
					else
					{
						throw new InvalidOperationException($"Cannot apply an indexer '[{indexValue}]' on a non-array JContainer (path: {current.Path}).");
					}
				}
			}

			return current;
		}
	}

	public bool DeleteConfig(string path, bool fromOverride = false)
	{
		//DeleteConfig(path, fromOverride ? _configLocalOverrides : _config);
		bool success = false;
		WriteConfig(config =>
		{
			success = DeleteConfig(path, config);
		}, fromOverride);
		return success;
	} 

	public bool DeleteConfig(string path, JObject config)
	{
		lock (_configLock)
		{
			return DeletePropertyAtSubPath(config, path);
		}

		/// <summary>
		/// Deletes a property or element at a specified JSON path.
		/// </summary>
		/// <param name="json">The root JObject.</param>
		/// <param name="path">The path to the item to delete (e.g., "user.address.zip" or "items[0]").</param>
		/// <returns>True if the item was found and removed, false otherwise.</returns>
		static bool DeletePropertyAtSubPath(JObject json, string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			var tokenToRemove = json.SelectToken(path);
			// Item not found
			if (tokenToRemove == null) return false;


			JContainer parent = tokenToRemove.Parent;
			// This case should only happen if we try to delete the root JObject itself, which is not allowed
			if (parent == null) return false;

			if (parent is JProperty parentProp)
			{
				parentProp.Remove();
				return true;
			}
			
			// Remove from JObject by property name
			if (parent is JObject parentObject)
			{
				// We need the property name that was used to access the tokenToRemove
				var propertyName = ((JProperty)tokenToRemove.Parent)!.Name;
				return parentObject.Remove(propertyName);
			}
			
			// Remove from JArray by value/reference
			if (parent is JArray)
			{
				tokenToRemove.Remove();
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Use this in conjunction with <see cref="FlushLocalOverrides"/> to flush a configuration setting that will NOT be version controlled AND takes precedence over the <see cref="CFG_FILE_NAME"/>.
	/// </summary>
	// public string SetLocalOverride(string key, string value) => SetConfig(key, value, true);
	// public string WriteLocalOverride(string key, string value) => WriteConfig(key, value, true);

	public bool DeleteLocalOverride(string key) => DeleteConfig(key, true);

	public string WriteConfigString(string key, string value) => WriteConfig(key, value);

	private static object _flushConfigLock = new();
	private static object _configLock = new();
	private void FlushConfig(JObject config, string path, bool createSubDirs)
	{
		// Because we have the CLI server, this block of code that writes to the config file must be guarded so that it 
		// doesn't accidentally run at the same time by the CLI server.
		lock (_flushConfigLock)
		{
			if (string.IsNullOrEmpty(path))
				throw new CliException("No beamable project exists. Please use beam init");

			if (TryGetProjectBeamableCLIVersion(out var version))
				SetConfig(CFG_JSON_FIELD_CLI_VERSION, version, config);

			// Flush the config
			var json = JsonConvert.SerializeObject(config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
			if (json == "null")
			{
				throw new Exception("Beamable config json panic! A 'null' value should never be written to disk, so this command is terminating. Please report this error to Beamable immediately. ");
			}

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			if (createSubDirs)
			{
				if (!Directory.Exists(Path.Combine(path, SHARED_FOLDER_NAME)))
				{
					Directory.CreateDirectory(Path.Combine(path, SHARED_FOLDER_NAME));
				}

				if (!Directory.Exists(Path.Combine(path, LOCAL_FOLDER_NAME)))
				{
					Directory.CreateDirectory(Path.Combine(path, LOCAL_FOLDER_NAME));
				}

				if (!Directory.Exists(Path.Combine(path, TEMP_FOLDER_NAME)))
				{
					Directory.CreateDirectory(Path.Combine(path, TEMP_FOLDER_NAME));
				}
			}

			string fullPath = Path.Combine(path, CFG_FILE_NAME);

			// We have to do this so that we don't collide with other software editing the files on disk.
			var written = false;
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var ex = default(Exception);
			while (!written)
			{
				// Timeout for edge cases
				if (stopwatch.ElapsedMilliseconds > 5000)
					break;

				try
				{
					LockedWrite(fullPath, json);
					written = true;
				}
				catch (IOException e)
				{
					ex = e;
					written = false;
				}
			}

			if (!written) throw new CliException($"Failed to flush configuration. LAST_EXCEPTION={ex}");
		}
	}
	
	#endregion

	#region VCS Integration

	/// <summary>
	/// This will attempt to create a ignore file for one of our supported VCSs. It tries to infer the VCS from the directory structure.
	/// </summary>	
	public void CreateIgnoreFile(Vcs? system = null, bool forceCreate = false)
	{
		if (string.IsNullOrEmpty(ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");

		system ??= BeamableWorkspaceVcs;
		var ignoreFilePath = system switch
		{
			Vcs.Git => Path.Combine(ConfigDirectoryPath, ".gitignore"),
			Vcs.Svn => Path.Combine(ConfigDirectoryPath, ".svnignore"),
			Vcs.Perforce => Path.Combine(ConfigDirectoryPath, ".p4ignore"),
			_ => ""
		};

		if (string.IsNullOrEmpty(ignoreFilePath))
		{
			Log.Information($"Current workspace is not inside a version control system --- you can run the {nameof(GenerateIgnoreFileCommand)} later to generate this when you chose your VCS system.");
			return;
		}

		if (File.Exists(ignoreFilePath) && !forceCreate)
		{
			return;
		}

		var builder = new StringBuilder();
		builder.Append(TEMP_FOLDER_NAME);
		builder.Append("/**/*");
		builder.Append(Environment.NewLine);

		builder.Append(LOCAL_FOLDER_NAME);
		builder.Append("/**/*");
		builder.Append(Environment.NewLine);

		File.WriteAllText(ignoreFilePath, builder.ToString());

		Log.Debug($"Generated ignore file at {ignoreFilePath}");
	}

	/// <summary>
	/// Traverses up the directory tree from the given path to find the root of a VCS repository.
	/// </summary>
	/// <param name="directoryPath">The starting directory path.</param>
	/// <returns>The VcsType (Git, Svn, Perforce, or None) if found.</returns>
	public static Vcs GetVcsType(string directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return Vcs.None;
		}

		DirectoryInfo currentDir;
		try
		{
			// Start with a valid DirectoryInfo object
			currentDir = new DirectoryInfo(directoryPath);

			// Ensure the starting directory exists.
			// If not, we can't traverse up from it.
			if (!currentDir.Exists)
			{
				// You could also try to get the parent of the non-existent path,
				// but returning None is safer.
				return Vcs.None;
			}
		}
		catch (Exception ex)
			when (ex is SecurityException ||
			      ex is ArgumentException ||
			      ex is PathTooLongException)
		{
			// Invalid path (format, permissions, or length)
			Console.WriteLine($"Error initializing path: {ex.Message}");
			return Vcs.None;
		}


		try
		{
			// Loop until we reach the root of the drive (parent is null)
			while (currentDir != null)
			{
				// 1. Check for Git
				// Git repositories have a .git directory at their root.
				if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
				{
					return Vcs.Git;
				}

				// 2. Check for Subversion
				// SVN checkouts have a .svn directory at their root.
				if (Directory.Exists(Path.Combine(currentDir.FullName, ".svn")))
				{
					return Vcs.Svn;
				}

				// 3. Check for Perforce
				// Perforce workspaces are often marked by a .p4config file
				// or sometimes a .p4 directory. This check is heuristic,
				// as Perforce setup can also be defined by environment variables.
				if (File.Exists(Path.Combine(currentDir.FullName, ".p4config")) ||
				    Directory.Exists(Path.Combine(currentDir.FullName, ".p4")))
				{
					return Vcs.Perforce;
				}

				// Move to the parent directory
				currentDir = currentDir.Parent;
			}
		}
		catch (SecurityException)
		{
			// We don't have permission to access a directory in the hierarchy.
			// We can't know the VCS, so return None.
			return Vcs.None;
		}
		catch (UnauthorizedAccessException)
		{
			// Same as above.
			return Vcs.None;
		}

		// Reached the root of the filesystem without finding a known VCS.
		return Vcs.None;
	}

	#endregion

	#region Helpers - Docker Paths

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


	public string GetPathFromRelativeToService(string path, string servicePath)
	{
		var relativePath = Path.GetDirectoryName(path);
		var fullPath = Path.Combine(servicePath, relativePath!);
		return GetRelativeToDockerBuildContextPath(fullPath);
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

	/// <summary>
	/// Get the docker build context path, which is used to copy files through the Dockerfile
	/// </summary>
	/// <returns></returns>
	public string GetAbsoluteDockerBuildContextPath()
	{
		var path = BeamableWorkspace;
		if (string.IsNullOrEmpty(path))
		{
			path = _workingDirectory;
		}

		return Path.GetFullPath(path);
	}

	#endregion

	#region Helpers - Otel Settings

	public OtelConfig LoadOtelConfigFromFile()
	{
		var config = GetConfig2(CFG_JSON_FIELD_OBJ_OTEL, new OtelConfig());
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
		WriteConfig(CFG_JSON_FIELD_OBJ_OTEL, config);
	}

	public bool ExistsOtelConfig() => GetConfig2<OtelConfig>(CFG_JSON_FIELD_OBJ_OTEL, null) != null;

	#endregion

	#region Helpers - Microservice Parsing Settings

	public List<string> LoadExtraPathsFromFile() => GetConfig2(CFG_JSON_FIELD_ARR_ADDITIONAL_PROJECT_PATHS, new List<string>());

	public List<string> LoadPathsToIgnoreFromFile()
	{
		var paths = GetConfig2(CFG_JSON_FIELD_ARR_IGNORED_PROJECT_PATHS, new List<string>());
		return paths
			.Select(GetRelativeToExecutionPath)
			.Select(Path.GetFullPath)
			.ToList();
	}

	public void SaveExtraPathsToFile(List<string> paths)
	{
		var currentPaths = GetConfig2<List<string>>(CFG_JSON_FIELD_ARR_ADDITIONAL_PROJECT_PATHS, new List<string>());
		currentPaths.AddRange(paths);
		WriteConfig(CFG_JSON_FIELD_ARR_ADDITIONAL_PROJECT_PATHS, currentPaths.Distinct().ToList());
	}

	public void SavePathsToIgnoreToFile(List<string> paths)
	{
		var currentPaths = GetConfig2<List<string>>(CFG_JSON_FIELD_ARR_IGNORED_PROJECT_PATHS, new List<string>());
		currentPaths.AddRange(paths);
		WriteConfig(CFG_JSON_FIELD_ARR_IGNORED_PROJECT_PATHS, currentPaths.Distinct().ToList());
	}

	public string GetProjectRootPath()
	{
		var relativePath = GetConfig2<string>(CFG_JSON_FIELD_PROJ_PATH_ROOT, ".");
		var fullPath = Path.Combine(BeamableWorkspace, relativePath);
		return new DirectoryInfo(fullPath).FullName;
	}

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

	#endregion

	#region Helpers - Microservice Codegen Settings

	public EngineProjectData GetLinkedEngineProjects() => GetConfig2(CFG_JSON_FIELD_OBJ_LINKED_ENGINE_PROJECTS, new EngineProjectData());

	public void SetLinkedEngineProjects(EngineProjectData data)
	{
		_ = WriteConfig(CFG_JSON_FIELD_OBJ_LINKED_ENGINE_PROJECTS, data);
	}

	#endregion

	#region Helpers - Login

	public bool ReadTokenFromFile(out CliToken response)
	{
		response = null;
		var fullPath = GetConfigPath(CFG_TOKEN_FILE_DIR);
		if (!File.Exists(fullPath))
			return false;

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
		var fullPath = GetConfigPath(CFG_TOKEN_FILE_DIR);

		var dir = Path.GetDirectoryName(fullPath);
		Directory.CreateDirectory(dir!);

		var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented });
		File.WriteAllText(fullPath, json);
	}

	public void DeleteTokenFile()
	{
		string fullPath = GetConfigPath(CFG_TOKEN_FILE_DIR);
		if (File.Exists(fullPath)) File.Delete(fullPath);
	}

	#endregion

	#region Utilities

	/// <summary>
	/// Utility function that goes up from the relative path looking for a folder with the name <see cref="CFG_FOLDER"/>.
	/// </summary>
	public static bool TryToFindBeamableFolder(string relativePath, out string result)
	{
		result = string.Empty;
		var basePath = relativePath;
		if (Directory.Exists(Path.Combine(basePath, CFG_FOLDER)))
		{
			result = Path.Combine(basePath, CFG_FOLDER);
			return true;
		}

		var parentDir = Directory.GetParent(basePath);
		while (parentDir != null)
		{
			var path = Path.Combine(parentDir.FullName, CFG_FOLDER);
			if (Directory.Exists(path))
			{
				result = path;
				return true;
			}

			parentDir = parentDir.Parent;
		}

		return false;
	}

	public static void LockedWrite(string path, string content, int allowedAttempts = 10, int retryDelayMs = 25)
	{
		for (var i = 0; i < allowedAttempts; i++)
		{
			try
			{
				using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
				using var writer = new StreamWriter(stream);
				writer.Write(content);
				writer.Flush();
			}
			catch (IOException) when (i < allowedAttempts)
			{
				Thread.Sleep(retryDelayMs);
			}
		}
	}
	public static string LockedRead(string path, int allowedAttempts=10, int retryDelayMs=25)
	{
		for (var i = 0; i < allowedAttempts ; i ++)
		{
			try
			{
				using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				using var reader = new StreamReader(stream);
				return reader.ReadToEnd();
			}
			catch (IOException) when (i < allowedAttempts)
			{
				Thread.Sleep(retryDelayMs);
			}
		}
		throw new IOException($"Failed to read file after {allowedAttempts} attempts.");
	}

	/// <summary>
	/// Takes in a folder path and tries to load
	/// </summary>
	/// <param name="folderPath"></param>
	/// <param name="result"></param>
	/// <exception cref="CliException"></exception>
	private static bool ReadConfigFile(string folderPath, bool isOptional, bool enforceRequiredFields, out JObject result)
	{
		string fullPath = Path.Combine(folderPath, CFG_FILE_NAME);
		result = new();
		if (!File.Exists(fullPath))
		{
			if (isOptional)
			{
				return true;
			}

			BeamableLogger.LogWarning($"Config file was not found at {fullPath}!");
			return false;
		}

		var content = LockedRead(fullPath);
		// var content = File.ReadAllText(fullPath);

		try
		{
			var read = JsonConvert.DeserializeObject<JObject>(content);
			if (read == null)
			{
				// do not set the result to a null value, ever. 

				if (isOptional)
				{
					return true;
				}

				BeamableLogger.LogWarning($"Config file was empty at {fullPath}!");
				return false;
			}

			result = read;
			return true;
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Takes in a "Config" dictionary (mapped to <see cref="CFG_FILE_NAME"/> contents) and see if it has all the required keys for the CLI to function at all.
	/// </summary>
	public static bool IsConfigValid(JObject dict, bool enforceRequiredFields, out List<string> missingRequiredKeys)
	{
		missingRequiredKeys = new[] { CFG_JSON_FIELD_HOST, CFG_JSON_FIELD_CID, }.ToList();
		if (dict == null || (dict.Count == 0 && enforceRequiredFields))
			return false;

		missingRequiredKeys = missingRequiredKeys.Except(dict.Properties().Select(p => p.Name)).ToList();
		return !enforceRequiredFields || missingRequiredKeys.Count == 0;
	}


	/// <summary>
	/// Takes a directory and builds out a Tarball with its contents.
	/// </summary>
	public static MemoryStream CreateInMemoryTarball(string sourceDirectory)
	{
		if (!Directory.Exists(sourceDirectory))
		{
			Console.WriteLine("Source directory does not exist.");
			return new MemoryStream();
		}

		// 1. Create the in-memory tarball
		var memoryStream = new MemoryStream();
		// The 'true' for includeBaseDirectory creates a tar entry for the source directory itself
		// if you want the contents directly in the root of the tar, use 'false'.
		TarFile.CreateFromDirectory(sourceDirectory, memoryStream, includeBaseDirectory: false);
		Log.Debug($"Tarball created in memory. Size: {memoryStream.Length} bytes.");

		return memoryStream;
	}

	/// <summary>
	/// Given a <paramref name="targetDir"/> and a tarball generated with <see cref="CreateInMemoryTarball"/>, replace the contents
	/// of the directory with the contents of the tarball.
	/// </summary>
	public static void ReplaceDirectoryWithTarball(string targetDir, MemoryStream memoryStream)
	{
		// Reset the stream position to the beginning for reading/extraction
		memoryStream.Position = 0;

		// 2. Clear the original directory
		Log.Debug($"Clearing directory: {targetDir}");
		Directory.Delete(targetDir, true);

		// 3. Extract the tarball contents back into the same directory
		Log.Debug($"Extracting tarball contents to: {targetDir}");
		// Ensure the directory exists before extraction
		Directory.CreateDirectory(targetDir);
		TarFile.ExtractToDirectory(memoryStream, targetDir, overwriteFiles: true);

		Log.Debug("Directory contents replaced with the tarball contents.");
	}

	#endregion
}

[Serializable]
public enum Vcs
{
	None,

	Git,
	Svn,
	Perforce,
}

[Serializable]
public class OtelConfig
{
	public bool BeamCliAllowTelemetry;
	public string BeamCliTelemetryLogLevel;
	public long BeamCliTelemetryMaxSize;
}

[Serializable]
public class EngineProjectData
{
	public HashSet<Unity> unityProjectsPaths = new();

	public HashSet<Unreal> unrealProjectsPaths = new();

	[Serializable]
	public struct Unity : IEquatable<string>, IEquatable<Unity>
	{
		/// <summary>
		/// Path to the entire project folder from the .beamable folder.
		/// </summary>
		public string Path;

		public bool Equals(Unity other) => Path == other.Path;
		public bool Equals(string other) => Path == other;

		public override int GetHashCode() => (Path != null ? Path.GetHashCode() : 0);

		public override bool Equals(object obj) => (obj is Unreal unity && Equals(unity)) ||
		                                           (obj is string unityPath && Equals(unityPath));

		public static bool operator ==(Unity left, Unity right) => left.Equals(right);

		public static bool operator !=(Unity left, Unity right) => !left.Equals(right);
	}

	[Serializable]
	public struct Unreal : IEquatable<string>, IEquatable<Unreal>
	{

		public const string CORE_NAME_SUFFIX = "MicroserviceClients";
		public const string BP_CORE_NAME_SUFFIX = "MicroserviceClientsBp";
		
		/// <summary>
		/// Name for the project's core module (the module every other module has access to).
		/// This will be used to generate the ______API UE Macros for the generated types.
		/// </summary>
		public string CoreProjectName;

		/// <summary>
		/// Name for the project's blueprint nodes module (the module every other module has access to).
		/// This will be used to generate the ______API UE Macros for the generated blueprint nodes types.
		///
		/// This is always <see cref="CoreProjectName"/> + "BlueprintNodes".
		/// </summary>
		public string BlueprintNodesProjectName;

		/// <summary>
		/// Path to the entire project folder from the .beamableFolder.
		/// </summary>
		public string Path;

		/// <summary>
		/// Path, relative to <see cref="Path"/>, to the "Source" directory of the project.
		/// </summary>
		public string SourceFilesPath;

		/// <summary>
		/// Path relative to <see cref="SourceFilesPath"/> for where we should put the Autogen folder for header files.
		/// </summary>
		public string MsCoreHeaderPath;

		/// <summary>
		/// Path relative to <see cref="SourceFilesPath"/> for where we should put the Autogen folder for cpp files.
		/// </summary>
		public string MsCoreCppPath;

		/// <summary>
		/// Path relative to <see cref="SourceFilesPath"/> for where we should put the Autogen folder for header blueprint node files.
		/// </summary>
		public string MsBlueprintNodesHeaderPath;

		/// <summary>
		/// Path relative to <see cref="SourceFilesPath"/> for where we should put the Autogen folder for cpp blueprint node files.
		/// </summary>
		public string MsBlueprintNodesCppPath;

		/// <summary>
		/// Path to the <see cref="PreviousGenerationPassesData"/> for the client's current plugin.
		/// </summary>
		public string BeamableBackendGenerationPassFile;

		public ReplacementTypeInfo[] ReplacementTypeInfos;

		public string GetProjectName()
		{
			return CoreProjectName.Remove(CoreProjectName.Length - CORE_NAME_SUFFIX.Length);
		}

		public bool Equals(string other) => Path.Equals(other);
		public bool Equals(Unreal other) => Path == other.Path;

		public override bool Equals(object obj) => (obj is Unreal unreal && Equals(unreal)) ||
		                                           (obj is string unrealPath && Equals(unrealPath));

		public override int GetHashCode() => (Path != null ? Path.GetHashCode() : 0);

		public static bool operator ==(Unreal left, Unreal right) => left.Equals(right);
		public static bool operator !=(Unreal left, Unreal right) => !(left == right);
		
		public static string GetCoreName(string projectName) => $"{projectName}{CORE_NAME_SUFFIX}";
		public static string GetBlueprintNodesProjectName(string projectName) => $"{projectName}{BP_CORE_NAME_SUFFIX}";
		
		
	}
}
