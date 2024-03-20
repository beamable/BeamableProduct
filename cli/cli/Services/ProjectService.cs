using Beamable.Common.Semantics;
using cli.Commands.Project;
using cli.Dotnet;
using cli.Unreal;
using CliWrap;
using Microsoft.CodeAnalysis.Sarif;
using Serilog;
using Spectre.Console;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Services;

public class ProjectData
{
	public HashSet<string> unityProjectsPaths = new HashSet<string>();

	public HashSet<Unreal> unrealProjectsPaths = new HashSet<Unreal>();

	public struct Unreal : IEquatable<string>, IEquatable<Unreal>
	{
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

		public bool Equals(string other) => Path.Equals(other);
		public bool Equals(Unreal other) => Path == other.Path;

		public override bool Equals(object obj) => (obj is Unreal unreal && Equals(unreal)) ||
												   (obj is string unrealPath && Equals(unrealPath));

		public override int GetHashCode() => (Path != null ? Path.GetHashCode() : 0);

		public static bool operator ==(Unreal left, Unreal right) => left.Equals(right);
		public static bool operator !=(Unreal left, Unreal right) => !(left == right);
	}
}

public class ProjectService
{
	private const string UNINSTALL_COMMAND = "new --uninstall";
	private readonly ConfigService _configService;
	private readonly VersionService _versionService;
	private readonly IAppContext _app;

	private ProjectData _projects;

	public bool? ConfigFileExists { get; }

	public ProjectService(ConfigService configService, VersionService versionService, IAppContext app)
	{
		_configService = configService;
		_versionService = versionService;
		_app = app;
		ConfigFileExists = _configService.DirectoryExists;
		_projects = configService.LoadDataFile<ProjectData>(Constants.CONFIG_LINKED_PROJECTS);
	}

	public List<string> GetLinkedUnityProjects()
	{
		return _projects.unityProjectsPaths.ToList();
	}

	public List<ProjectData.Unreal> GetLinkedUnrealProjects()
	{
		return _projects.unrealProjectsPaths.ToList();
	}

	public void AddUnityProject(string relativePath)
	{
		_projects.unityProjectsPaths.Add(relativePath);
		_configService.SaveDataFile(Constants.CONFIG_LINKED_PROJECTS, _projects);
	}

	public void AddUnrealProjectWithOss(string projectPath)
	{
		// Always ensure that we store things relative to the root of the UE project (not the repo)
		var unrealRootPath = EnsureUnrealRootPath(projectPath);

		// Ensure we have the OSS as a plugin.
		var foundOss = unrealRootPath.GetDirectories("Plugins/OnlineSubsystemBeamable").Any();
		if (!foundOss) throw new CliException("The selected UE project does not contain the OnlineSubsystemBeamable plugin. You should never see this. If you do, report a bug.");

		// Find beamable folder (it must exist either as a parent of the UE project root OR inside the UE project root folder.
		var beamableFolderPath = FindBeamableFolderPath(unrealRootPath);

		// The path must always be stored relative to the .beamable folder as we run commands always through that.
		projectPath = Path.GetRelativePath(beamableFolderPath.ToString(), unrealRootPath.ToString());
		projectPath = projectPath.StartsWith(".") ? projectPath.Substring(1) : projectPath;
		projectPath = projectPath.StartsWith("/") ? projectPath.Substring(1) : projectPath;

		// Configure the ProjectData for Unreal
		var projData = new ProjectData.Unreal();
		projData.Path = projectPath;
		projData.SourceFilesPath = projectPath + "Plugins/OnlineSubsystemBeamable/Source/";

		projData.CoreProjectName = "OnlineSubsystemBeamable";
		projData.BlueprintNodesProjectName = "OnlineSubsystemBeamableBp";

		// These are defined relative to the SourceFilesPath
		projData.MsCoreHeaderPath = projData.CoreProjectName + "/Public/Customer/";
		projData.MsCoreCppPath = projData.CoreProjectName + "/Private/Customer/";
		projData.MsBlueprintNodesHeaderPath = projData.BlueprintNodesProjectName + "/Public/Customer/";
		projData.MsBlueprintNodesCppPath = projData.BlueprintNodesProjectName + "/Private/Customer/";

		projData.BeamableBackendGenerationPassFile = projectPath + $"Plugins/BeamableCore/Source/{UnrealSourceGenerator.currentGenerationPassDataFilePath}.json";

		// Save it
		_projects.unrealProjectsPaths.Add(projData);
		_configService.SaveDataFile(Constants.CONFIG_LINKED_PROJECTS, _projects);
	}

	public void AddUnrealProject(string projectPath, string msClientModuleName, string blueprintNodesModuleName, bool msClientModuleIsPublicPrivate, bool blueprintNodesModuleIsPublicPrivate)
	{
		// Always ensure that we store things relative to the root of the UE project (not the repo)
		var unrealRootPath = EnsureUnrealRootPath(projectPath);

		// Ensure we DON'T have the OSS as a plugin.
		var foundOss = unrealRootPath.GetDirectories("Plugins/OnlineSubsystemBeamable").Any();
		if (foundOss) throw new CliException("The selected UE project contains the OnlineSubsystemBeamable plugin. If you're a customer seeing this, please report a bug.");

		// Find beamable folder (it must exist either as a parent of the UE project root OR inside the UE project root folder.
		var beamableFolderPath = FindBeamableFolderPath(unrealRootPath);

		// The path must always be stored relative to the .beamable folder as we run commands always through that.
		projectPath = Path.GetRelativePath(beamableFolderPath.ToString(), unrealRootPath.ToString());
		projectPath = projectPath.StartsWith(".") ? projectPath.Substring(1) : projectPath;
		projectPath = projectPath.StartsWith("/") ? projectPath.Substring(1) : projectPath;

		var projData = new ProjectData.Unreal() { };

		var pathToBackendGenerationJson = $"Plugins/BeamableCore/Source/{UnrealSourceGenerator.currentGenerationPassDataFilePath}.json";
		projData.Path = projectPath;
		projData.SourceFilesPath = projectPath + (string.IsNullOrEmpty(projectPath) ? "Source/" : "/Source/");

		projData.CoreProjectName = msClientModuleName;
		projData.BlueprintNodesProjectName = blueprintNodesModuleName;

		// These are defined relative to the SourceFilesPath
		projData.MsCoreHeaderPath = projData.CoreProjectName + (msClientModuleIsPublicPrivate ? "/Public/" : "/");
		projData.MsCoreCppPath = projData.CoreProjectName + (msClientModuleIsPublicPrivate ? "/Private/" : "/");
		projData.MsBlueprintNodesHeaderPath = projData.BlueprintNodesProjectName + (blueprintNodesModuleIsPublicPrivate ? "/Public/" : "/");
		projData.MsBlueprintNodesCppPath = projData.BlueprintNodesProjectName + (blueprintNodesModuleIsPublicPrivate ? "/Private/" : "/");

		projData.BeamableBackendGenerationPassFile = projectPath + pathToBackendGenerationJson;

		_projects.unrealProjectsPaths.Add(projData);
		_configService.SaveDataFile(Constants.CONFIG_LINKED_PROJECTS, _projects);
	}

	private static DirectoryInfo EnsureUnrealRootPath(string projectPath)
	{
		var unrealRootPath = new DirectoryInfo(projectPath);
		var isUnrealRoot = unrealRootPath.GetFiles().Any(f => f.Extension.Contains(".uproject"));
		// We expect the path given here to be an unreal root path.
		if (!isUnrealRoot) throw new CliException("The selected path is not the root of an Unreal project folder. (If you're a customer seeing this, report a bug).");
		return unrealRootPath;
	}

	private static DirectoryInfo FindBeamableFolderPath(DirectoryInfo unrealRootPath)
	{
		var parent = unrealRootPath;
		bool containsBeamableFolder;
		do
		{
			containsBeamableFolder = (bool)parent?.GetDirectories().Any(d => d.Name.EndsWith(".beamable"));
			parent = !containsBeamableFolder ? parent.Parent : parent;
		} while (parent != null && !containsBeamableFolder);

		// You should not see this ever, but... if you do; the error explains the problem. 
		if (!containsBeamableFolder)
		{
			throw new CliException(
				"There is no .beamable folder as a parent of this UE project (or inside it)." +
				" Please run the CLI from a directory inside your UE project root OR from a parent folder that is also inside your source-control repo.");
		}

		return parent;
	}


	public async Task EnsureCanUseTemplates(string version, bool quiet = false)
	{
		var info = await GetTemplateInfo();

		if (!info.HasTemplates ||
			!string.Equals(version, info.templateVersion, StringComparison.CurrentCultureIgnoreCase))
		{
			await PromptAndInstallTemplates(info.templateVersion, version, quiet);
		}
	}

	/// <param name="currentlyInstalledVersion">
	/// The current installed version of the templates
	/// A null string will imply there are no templates installed
	/// </param>
	/// <param name="version">
	/// The version of the template to install.
	/// A null string will imply the "latest" version.
	///
	/// <b> There are missing versions of the template! </b>
	/// See for details, https://www.nuget.org/packages/Beamable.Templates, but not all versions exist.
	/// This may cause the command to fail, but in that case, that is expected.
	/// </param>
	/// <param name="quiet">
	/// If true, it will skip asking in prompt if user wants to update to the latest version,
	/// and just assumes that it should install or update it.
	/// </param>
	private async Task PromptAndInstallTemplates(string currentlyInstalledVersion, string version, bool quiet = false)
	{
		// lets get user consent before auto installing beamable templates
		string question;
		bool noTemplatesInstalled = string.IsNullOrEmpty(currentlyInstalledVersion);
		if (noTemplatesInstalled)
		{
			question =
				"Beamable templates are currently not installed. Would you like to proceed with installing the Beamable templates?";
		}
		else
		{
			string latestMsg = string.IsNullOrEmpty(version) ? "the latest version" : $"version {version}";
			question =
				$"Beamable templates are currently installed as {currentlyInstalledVersion}. Would you like to proceed with installing {latestMsg}";
		}

		bool canInstallTemplates = quiet || AnsiConsole.Confirm(question);

		switch (canInstallTemplates)
		{
			case false when noTemplatesInstalled:
				throw new CliException(
					"Before you can continue, you must install the Beamable templates by running - " +
					"dotnet new --install beamable.templates");
			case false:
				return;
		}

		const string packageName = "beamable.templates";

		if (!string.IsNullOrEmpty(currentlyInstalledVersion))
		{
			// there are already templates installed, so un-install them first.
			await RunDotnetCommand($"{UNINSTALL_COMMAND} {packageName}");
		}

		var (result, installStream) = await CliExtensions.RunWithOutput(_app.DotnetPath, $"new --install {packageName}::{version}");
		var isTemplateInstalled = result.ExitCode == 0;

		if (!isTemplateInstalled)
		{
			Log.Verbose("[ExitCode:{ResultExitCode}] Command output: {InstallStream}", result.ExitCode, installStream);
			throw new CliException("Installation of Beamable templates failed, please attempt the installation again.");
		}
	}

	public async Task<DotnetTemplateInfo> GetTemplateInfo()
	{
		var (_, templateStream) = await CliExtensions.RunWithOutput(_app.DotnetPath, UNINSTALL_COMMAND);

		var info = new DotnetTemplateInfo();

		var buffer = templateStream.ToString().ReplaceLineEndings("\n");
		string pattern =
			@"Beamable\.Templates[\s\S]*?Version: (\d+\.\d+\.\d+(?:-\w+\.\w+\d*)?)[\s\S]*?Templates:\n((?:\s{3}.*\(.*\)\s+C#\n)+)";

		Regex regex = new Regex(pattern);

		Match match = regex.Match(buffer);
		if (match.Success)
		{
			string version = match.Groups[1].Value;
			info.templateVersion = version.Trim();

			string templates = match.Groups[2].Value;
			foreach (Match templateMatch in Regex.Matches(templates, @"\s{3}(.*\(.*\))\s+C#"))
			{
				string template = templateMatch.Groups[1].Value;
				info.templates.Add(template.Trim());
			}
		}


		// var lines = buffer.Split(Environment.NewLine);
		// for (var i = 0; i < lines.Length; i++)
		// {
		// 	var line = lines[i];
		// 	if (line.Contains("Beamable.Templates"))
		// 	{
		// 		
		// 	}
		// }

		return info;
	}

	public class DotnetTemplateInfo
	{
		public bool HasTemplates => !string.IsNullOrEmpty(templateVersion);
		public string templateVersion;
		public List<string> templates = new List<string>();
	}

	public class NewServiceInfo
	{
		public string SolutionDirectory => Path.GetDirectoryName(SolutionPath);
		public string SolutionPath;
		public string ServicePath;
	}

	public string GeneratePathForProject(string slnFilePath, string beamId)
	{
		var slnDirectory = Path.GetDirectoryName(slnFilePath)!;

		if (string.IsNullOrEmpty(slnDirectory))
		{
			slnDirectory = slnFilePath;
		}

		var rootServicesPath = Path.Combine(slnDirectory, "services");
		var path = _configService.GetRelativePath(Path.Combine(rootServicesPath, beamId));
		return path;
	}

	public async Task<NewServiceInfo> CreateNewStorage(NewStorageCommandArgs args)
	{
		string usedVersion = string.IsNullOrWhiteSpace(args.SpecifiedVersion) ? await GetVersion() : args.SpecifiedVersion;
		var microserviceInfo = new NewServiceInfo();
		// check that we have the templates available
		await EnsureCanUseTemplates(usedVersion, args.Quiet);
		microserviceInfo.SolutionPath = args.SlnFilePath;
		if (!args.GetSlnExists())
		{
			microserviceInfo.SolutionPath = await CreateNewSolution(args.GetSlnDirectory(), args.GetSlnFileName());
		}

		if (!_configService.IsPathInWorkingDirectory(microserviceInfo.SolutionPath))
		{
			throw new CliException(
				$"Solution file({microserviceInfo.SolutionPath}) should not exists outside working directory({_configService.WorkingDirectory}) or its subdirectories.");
		}
		if (!File.Exists(microserviceInfo.SolutionPath))
		{
			string correctSlnPath = string.Empty;
			string dir = Path.GetDirectoryName(microserviceInfo.SolutionPath);
			if (!string.IsNullOrWhiteSpace(dir))
			{
				var files = Directory.GetFiles(dir);
				var slnFiles = files.Where(f => f.EndsWith(".sln")).ToArray();
				if (slnFiles.Length == 1)
				{
					correctSlnPath = slnFiles[0];
				}
			}

			var exception = new CliException($"No sln file found at path=[{microserviceInfo.SolutionPath}]",
				Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_SOLUTION_NOT_FOUND, true,
				string.IsNullOrWhiteSpace(correctSlnPath) ? null : $"Try using \"{correctSlnPath}\" as --sln option value");

			throw exception;
		}

		if (string.IsNullOrWhiteSpace(args.ServicesBaseFolderPath))
		{
			var directory = Path.GetDirectoryName(microserviceInfo.SolutionPath);
			args.ServicesBaseFolderPath = Path.Combine(directory!, "services");
		}
		microserviceInfo.ServicePath = Path.Combine(args.ServicesBaseFolderPath, args.ProjectName);
		await RunDotnetCommand($"new beamstorage -n {args.ProjectName} -o {microserviceInfo.ServicePath}");
		await RunDotnetCommand($"sln {microserviceInfo.SolutionPath} add {microserviceInfo.ServicePath}");
		return microserviceInfo;
	}

	public async Task<NewServiceInfo> CreateNewMicroservice(NewMicroserviceArgs args)
	{
		// check that we have the templates available
		string usedVersion = string.IsNullOrWhiteSpace(args.SpecifiedVersion) ? await GetVersion() : args.SpecifiedVersion;
		await EnsureCanUseTemplates(usedVersion, args.Quiet);

		var microserviceInfo = new NewServiceInfo
		{
			SolutionPath = args.SlnFilePath
		};
		if (!args.GetSlnExists())
		{
			await CreateNewSolution(args.GetSlnDirectory(), args.GetSlnFileName());
		}

		if (string.IsNullOrWhiteSpace(args.ServicesBaseFolderPath))
		{
			var directory = Path.GetDirectoryName(microserviceInfo.SolutionPath);
			args.ServicesBaseFolderPath = Path.Combine(directory!, "services");
		}

		if (!_configService.IsPathInWorkingDirectory(microserviceInfo.SolutionPath))
		{
			throw new CliException(
				$"Solution file({microserviceInfo.SolutionPath}) should not exists outside working directory({_configService.WorkingDirectory}) or its subdirectories.");
		}

		microserviceInfo.ServicePath = await CreateNewService(microserviceInfo.SolutionPath, args.ProjectName, args.ServicesBaseFolderPath, usedVersion);
		return microserviceInfo;
	}

	public async Task<string> CreateNewSolution(string directory, string solutionName)
	{
		var solutionPath = Path.Combine(_configService.WorkingDirectory, directory);

		if (Directory.Exists(solutionPath))
		{
			if (Directory.EnumerateFiles(solutionPath, "*sln").ToArray().Length > 0)
			{
				throw new CliException("Cannot create a solution because the directory already exists and it contains a solution file.");
			}
		}

		// create the solution
		await RunDotnetCommand($"new sln -n \"{solutionName}\" -o \"{solutionPath}\"");

		return Path.Combine(solutionPath, $"{solutionName}.sln");
	}

	public async Task<string> CreateNewService(string solutionPath, string projectName, string rootServicesPath, string version)
	{
		if (!File.Exists(solutionPath))
		{
			throw new CliException($"{solutionPath} does not exist");
		}
		var commonProjectName = $"{projectName}Common";
		var projectPath = Path.Combine(rootServicesPath, projectName);
		var commonProjectPath = Path.Combine(rootServicesPath, commonProjectName);

		// create the beam microservice project
		await RunDotnetCommand($"new beamservice -n \"{projectName}\" -o \"{projectPath}\"");

		// restore the microservice tools
		await RunDotnetCommand(
			$"tool restore --tool-manifest \"{Path.Combine(projectName, ".config", "dotnet-tools.json")}\"");

		// add the microservice to the solution
		await RunDotnetCommand($"sln \"{solutionPath}\" add \"{projectPath}\"");


		await UpdateProjectDependencyVersion(projectPath, "Beamable.Microservice.Runtime", version);

		// create the shared library project only if requested
		await CreateCommonProject(commonProjectName, commonProjectPath, version, solutionPath);
		// add the shared library as a reference of the project
		await RunDotnetCommand($"add \"{projectPath}\" reference \"{commonProjectPath}\"");


		return projectPath;
	}

	/// <summary>
	/// Creates a new common project.
	/// </summary>
	/// <param name="commonProjectName">The name of the common project.</param>
	/// <param name="commonProjectPath">The path where the common project should be created.</param>
	/// <param name="usedVersion">The version to be used for the common project.</param>
	/// <returns>Task representing the asynchronous operation.</returns>
	public async Task CreateCommonProject(string commonProjectName, string commonProjectPath, string usedVersion, string solutionPath)
	{
		await RunDotnetCommand($"new beamlib -n \"{commonProjectName}\" -o \"{commonProjectPath}\"");

		// restore the shared library tools
		await RunDotnetCommand(
			$"tool restore --tool-manifest \"{Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}\"");
		if (!string.IsNullOrWhiteSpace(usedVersion))
		{
			await UpdateProjectDependencyVersion(commonProjectPath, "Beamable.Common", usedVersion);
		}

		// add the shared library to the solution
		await RunDotnetCommand($"sln \"{solutionPath}\" add \"{commonProjectPath}\"");
	}

	/// <summary>
	/// Runs a dotnet command that will add or update dependency in specified dotnet project.
	/// </summary>
	/// <param name="projectPath">dotnet project path</param>
	/// <param name="packageName">Name of package to update</param>
	/// <param name="version">Specifies in which version package will be installed.
	/// Can be empty- then it will install latest available version.</param>
	/// <returns></returns>
	private Task UpdateProjectDependencyVersion(string projectPath, string packageName, string version)
	{
		var versionToUpdate = string.IsNullOrWhiteSpace(version) || version.Equals("0.0.0")
			? string.Empty
			: $" --version \"{version}\"";

		return RunDotnetCommand($"add \"{projectPath}\" package {packageName}{versionToUpdate}");
	}

	public Task<BeamoServiceDefinition> AddDefinitonToNewService(SolutionCommandArgs args, NewServiceInfo info)
	{
		var serviceRelativePath = _configService.GetRelativePath(info.ServicePath);

		// Find path to service folders: either it is in the working directory, or it will be inside 'args.name\\services' from the working directory.
		string projectDirectory = Path.GetDirectoryName(serviceRelativePath);
		string projectDockerfilePath = Path.Combine(Path.GetFileName(serviceRelativePath), "Dockerfile");

		// now that a .beamable folder has been created, setup the beamo manifest
		return args.BeamoLocalSystem.AddDefinition_HttpMicroservice(args.ProjectName.Value,
			projectDirectory,
			projectDockerfilePath,
			CancellationToken.None,
			!args.Disabled,
			serviceRelativePath);
	}


	public async Task UpdateDockerFileWithCommonProject(ConfigService configService, string projectName, string dockerfilePath,
		string dockerBuildContextPath)
	{
		var commonProjectName = $"{projectName}Common";
		Log.Information("Docker file path is {DockerfilePath}", dockerfilePath);
		var serviceFolder = Path.GetDirectoryName(dockerfilePath);
		serviceFolder = configService.GetRelativePath(serviceFolder);
		Log.Information("Docker file folder is {DockerFileFolder}", serviceFolder);

		dockerfilePath = Path.Combine(configService.BaseDirectory, dockerBuildContextPath, dockerfilePath);
		var dockerfileText = await File.ReadAllTextAsync(dockerfilePath);

		const string search =
			"# <BEAM-CLI-INSERT-FLAG:COPY_COMMON> do not delete this line. It is used by the beam CLI to insert custom actions";
		var replacement = @$"WORKDIR /subsrc/{commonProjectName}
COPY {commonProjectName}/. .
{search}";
		dockerfileText = dockerfileText.Replace(search, replacement);
		await File.WriteAllTextAsync(dockerfilePath, dockerfileText);
	}

	public async Task LinkProjects(AddUnityClientOutputCommand addUnityCommand,
		AddUnrealClientOutputCommand addUnrealCommand, IServiceProvider provider)
	{
		// ask if we should link a Unity project
		var addUnityProject = AnsiConsole.Confirm(
			"Would you like to link a Unity project? A linked Unity project will receive autogenerated client updates.",
			true);
		if (addUnityProject)
		{
			await addUnityCommand.Handle(new AddProjectClientOutputCommandArgs { path = ".", Provider = provider });
		}

		// ask if we should link a Unreal project
		var addUnrealProject = AnsiConsole.Confirm(
			"Would you like to link an Unreal project? A linked Unreal project will receive autogenerated client updates.",
			true);
		if (addUnrealProject)
		{
			await addUnrealCommand.Handle(
				new UnrealAddProjectClientOutputCommandArgs() { path = ".", Provider = provider });
		}
	}

	private async Task<string> GetVersion()
	{
		var nugetPackages = (await _versionService.GetBeamableToolPackageVersions(replaceDashWithDot: false)).ToArray();

		return nugetPackages.Last().packageVersion;
	}

	Task RunDotnetCommand(string arguments)
	{
		return CliExtensions.GetDotnetCommand(_app.DotnetPath, arguments).ExecuteAsyncAndLog().Task;
	}

	public static async Task WatchBuild(BuildProjectCommandArgs args, ServiceName serviceName, Action<ProjectErrorReport> onReport)
	{
		var localServices = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols;
		if (!localServices.TryGetValue(serviceName, out var service))
		{
			throw new CliException(
				$"The given id=[{serviceName}] does not match any local services in the local beamo manifest.");
		}
		var canBeBuiltLocally = args.BeamoLocalSystem.VerifyCanBeBuiltLocally(serviceName);
		if (!canBeBuiltLocally)
		{
			throw new CliException($"The given id=[{serviceName}] cannot be build locally.");
		}

		var errorPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "buildLogs",
			$"{serviceName}.json");
		var errorDir = Path.GetDirectoryName(errorPath);
		Directory.CreateDirectory(errorDir);
		Log.Debug($"error log path=[{errorPath}]");
		var dockerfilePath = Path.Combine(args.ConfigService.GetRelativePath(service.DockerBuildContextPath),
			service.RelativeDockerfilePath);
		var projectPath = Path.GetDirectoryName(dockerfilePath);

		var watchPart = args.watch
			? $"watch -q --project {projectPath} build --"
			: $"build {projectPath}";
		var commandStr =
			$"{watchPart} -p:ErrorLog=\"{errorPath}%2Cversion=2\"";
		Log.Debug($"dotnet command=[{args.AppContext.DotnetPath} {commandStr}]");

		using var cts = new CancellationTokenSource();

		var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithEnvironmentVariables(new Dictionary<string, string>
			{
				["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1",
				["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
			})
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				if (line.Trim() == "dotnet watch : Waiting for a file to change before restarting dotnet...")
				{
					// read the data file!
					var report = ReadErrorReport(errorPath);
					onReport?.Invoke(report);
				}

				Log.Information(line);
			}))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync(cts.Token);


		await command;

		if (!args.watch)
		{
			var report = ReadErrorReport(errorPath);
			onReport?.Invoke(report);
		}
	}

	static ProjectErrorReport ReadErrorReport(string errorLogPath)
	{
		Log.Debug("Reading SARIF report at " + errorLogPath);
		try
		{
			var outputs = new List<ProjectErrorResult>();
			SarifLog log = SarifLog.Load(errorLogPath);
			foreach (var result in log.Results())
			{
				if (result.Level is FailureLevel.Note or FailureLevel.None) continue;

				var location = result.Locations.FirstOrDefault();

				outputs.Add(new ProjectErrorResult
				{
					level = result.Level.ToString(),
					formattedMessage = result.FormatForVisualStudio(),
					uri = location?.PhysicalLocation?.ArtifactLocation?.Uri?.ToString() ?? "",
					line = location?.PhysicalLocation?.Region?.StartLine ?? 0,
					column = location?.PhysicalLocation?.Region?.StartColumn ?? 0
				});
			}

			return new ProjectErrorReport
			{
				errors = outputs,
				isSuccess = outputs.Count == 0
			};
		}
		catch (Exception ex)
		{
			Log.Error($"Failed to read SARIF report. type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}] ");
			throw new CliException($"Failed to read SARIF report. type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}] ", 2, false);
		}
	}
}

public class ProjectErrorReport
{
	public bool isSuccess;
	public List<ProjectErrorResult> errors = new List<ProjectErrorResult>();
}
public class ProjectErrorResult
{
	public string level;
	public string formattedMessage;
	public string uri;
	public int line;
	public int column;
}

public static class CliExtensions
{
	public static async Task<(CommandResult, StringBuilder)> RunWithOutput(string dotnetPath, string arguments)
	{
		var builder = new StringBuilder();
		var result = await GetDotnetCommand(dotnetPath, arguments)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(builder))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(builder))
			.ExecuteAsync();

		return (result, builder);
	}

	public static Command GetDotnetCommand(string dotnetPath, string arguments)
	{
		var command = Cli.Wrap(dotnetPath)
			.WithEnvironmentVariables(new Dictionary<string, string> { ["DOTNET_CLI_UI_LANGUAGE"] = "en" })
			.WithArguments(arguments);
		return command;
	}

	public static CommandTask<CommandResult> ExecuteAsyncAndLog(this Command command)
	{
		Log.Information($"Running '{command.TargetFilePath} {command.Arguments}'");

		var buffer = new StringBuilder();
		var commandTask = command
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(buffer))
			.ExecuteAsync();
		commandTask.Task.ContinueWith(t =>
		{
			if (!t.IsCompletedSuccessfully)
			{
				Log.Error(buffer.ToString());
			}
		});
		return commandTask;
	}
}
