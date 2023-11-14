using Beamable.Common;
using Beamable.Common.Dependencies;
using cli.Dotnet;
using cli.Unreal;
using CliWrap;
using CliWrap.Buffered;
using JetBrains.Annotations;
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

	private ProjectData _projects;

	public bool? ConfigFileExists { get; }

	public ProjectService(ConfigService configService, VersionService versionService)
	{
		_configService = configService;
		_versionService = versionService;
		_projects = configService.LoadDataFile<ProjectData>(".linkedProjects");
		ConfigFileExists = _configService.ConfigFileExists;
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
		_configService.SaveDataFile(".linkedProjects", _projects);
	}

	public void AddUnrealProject(string projectPath, string msClientModuleName, string blueprintNodesModuleName, bool msClientModuleIsPublicPrivate, bool blueprintNodesModuleIsPublicPrivate)
	{
		var msHeaderPath = msClientModuleName;
		msHeaderPath += msClientModuleIsPublicPrivate ? "\\Public\\" : "\\";
		
		var msCppPath = msClientModuleName;
		msCppPath += msClientModuleIsPublicPrivate ? "\\Private\\" : "\\";
		
		var bpNodesHeaderPath = blueprintNodesModuleName;
		bpNodesHeaderPath += blueprintNodesModuleIsPublicPrivate ? "\\Public\\" : "\\";
		
		var bpNodesCppPath = blueprintNodesModuleName;
		bpNodesCppPath += blueprintNodesModuleIsPublicPrivate ? "\\Private\\" : "\\";
		
		_projects.unrealProjectsPaths.Add(new ProjectData.Unreal()
		{
			CoreProjectName = msClientModuleName,
			BlueprintNodesProjectName = blueprintNodesModuleName,
			Path = projectPath,
			SourceFilesPath = projectPath + $"\\Source\\",
			MsCoreHeaderPath = msHeaderPath,
			MsCoreCppPath = msCppPath,
			MsBlueprintNodesHeaderPath = bpNodesHeaderPath,
			MsBlueprintNodesCppPath = bpNodesCppPath,
			BeamableBackendGenerationPassFile = projectPath +
			                                    $"\\Plugins\\BeamableCore\\Source\\{UnrealSourceGenerator.currentGenerationPassDataFilePath}.json"
		});
		_configService.SaveDataFile(".linkedProjects", _projects);
	}

	public static async Task EnsureCanUseTemplates(string version)
	{
		var info = await GetTemplateInfo();

		if (!info.HasTemplates ||
		    !string.Equals(version, info.templateVersion, StringComparison.CurrentCultureIgnoreCase))
		{
			await PromptAndInstallTemplates(info.templateVersion, version);
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
	private static async Task PromptAndInstallTemplates(string currentlyInstalledVersion, string version)
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

		bool canInstallTemplates = AnsiConsole.Confirm(question);

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

		var installStream = new StringBuilder();
		var result = await CliExtensions.GetDotnetCommand($"new --install {packageName}::{version}")
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(installStream))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(installStream))
			.ExecuteAsyncAndLog().Task;
		var isTemplateInstalled = result.ExitCode == 0;

		if (!isTemplateInstalled)
		{
			Log.Verbose("[ExitCode:{ResultExitCode}] Command output: {InstallStream}", result.ExitCode, installStream);
			throw new CliException("Installation of Beamable templates failed, please attempt the installation again.");
		}
	}

	public static async Task<DotnetTemplateInfo> GetTemplateInfo()
	{
		var templateStream = new StringBuilder();

		await CliExtensions.GetDotnetCommand(UNINSTALL_COMMAND)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(templateStream))
			.ExecuteBufferedAsync();

		var info = new DotnetTemplateInfo();

		var buffer = templateStream.ToString().Replace("\r\n", "\n");
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

	public async Task AddProjectReference(string slnFilePath, string project, string projectReference)
	{
		var slnDirectory = Path.GetDirectoryName(slnFilePath);
		var rootServicesPath = Path.Combine(slnDirectory, "services");

		var projectPath = Path.Combine(rootServicesPath, project);
		var referencePath = Path.Combine(rootServicesPath, projectReference);
		await RunDotnetCommand($"add {projectPath} reference {referencePath}");
	}

	public async Task CreateNewStorage(string slnFilePath, string storageName)
	{
		var slnDirectory = Path.GetDirectoryName(slnFilePath);
		var rootServicesPath = Path.Combine(slnDirectory, "services");
		var storagePath = Path.Combine(rootServicesPath, storageName);

		if (Directory.Exists(storagePath))
		{
			throw new CliException("Cannot create a storage because the directory already exists");
		}

		await EnsureCanUseTemplates(null); // TODO: tech debt, this whole command needs to care about version.

		// create the beam microservice project
		await RunDotnetCommand($"new beamstorage -n {storageName} -o {storagePath}");

		// add the new project as a reference to the solution
		await RunDotnetCommand($"sln {slnFilePath} add {storagePath}");
	}

	public Task<string> CreateNewSolution(NewSolutionCommandArgs args)
	{
		return CreateNewSolution(args.directory, args.SolutionName, args.ProjectName,
			!args.SkipCommon, args.SpecifiedVersion);
	}

	public async Task<string> CreateNewSolution(string directory, string solutionName, string projectName,
		bool createCommonLibrary = true, string version = "")
	{
		if (string.IsNullOrEmpty(directory))
		{
			directory = solutionName;
		}

		string usedVersion = string.IsNullOrWhiteSpace(version) ? await GetVersion() : version;

		var solutionPath = Path.Combine(_configService.WorkingDirectory, directory);
		var rootServicesPath = Path.Combine(solutionPath, "services");
		var commonProjectName = $"{projectName}Common";
		var projectPath = Path.Combine(rootServicesPath, projectName);
		var commonProjectPath = Path.Combine(rootServicesPath, commonProjectName);

		if (Directory.Exists(solutionPath))
		{
			throw new CliException("Cannot create a solution because the directory already exists");
		}

		// check that we have the templates available
		await EnsureCanUseTemplates(usedVersion);

		// create the solution
		await RunDotnetCommand($"new sln -n \"{solutionName}\" -o \"{solutionPath}\"");

		// create the beam microservice project
		await RunDotnetCommand($"new beamservice -n \"{projectName}\" -o \"{projectPath}\"");

		// restore the microservice tools
		await RunDotnetCommand(
			$"tool restore --tool-manifest \"{Path.Combine(projectName, ".config", "dotnet-tools.json")}\"");

		// add the microservice to the solution
		await RunDotnetCommand($"sln \"{solutionPath}\" add \"{projectPath}\"");


		await UpdateProjectDependencyVersion(projectPath, "Beamable.Microservice.Runtime", usedVersion);

		// create the shared library project only if requested
		if (createCommonLibrary)
		{
			await RunDotnetCommand($"new beamlib -n \"{commonProjectName}\" -o \"{commonProjectPath}\"");

			// restore the shared library tools
			await RunDotnetCommand(
				$"tool restore --tool-manifest \"{Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}\"");

			// add the shared library to the solution
			await RunDotnetCommand($"sln \"{solutionPath}\" add \"{commonProjectPath}\"");

			// add the shared library as a reference of the project
			await RunDotnetCommand($"add \"{projectPath}\" reference \"{commonProjectPath}\"");

			await UpdateProjectDependencyVersion(commonProjectPath, "Beamable.Common", usedVersion);
		}

		return solutionPath;
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

	public Task<string> AddToSolution(SolutionCommandArgs args)
	{
		return args.ProjectService.AddToSolution(args.SolutionName, args.ProjectName, !args.SkipCommon,
			version: args.SpecifiedVersion);
	}

	public async Task<string> AddToSolution(string solutionName, string projectName, bool createCommonLibrary = true,
		bool skipSolutionCreation = false, string version = "")
	{
		var solutionFile = $"{solutionName}.sln";
		var solutionPath = Path.Combine(_configService.WorkingDirectory, solutionFile);
		var rootServicesPath = "services";
		var commonProjectName = $"{projectName}Common";
		var projectPath = Path.Combine(rootServicesPath, projectName);
		var commonProjectPath = Path.Combine(rootServicesPath, commonProjectName);

		if (Directory.Exists(projectPath))
		{
			throw new CliException($"Project {projectName} already exists");
		}

		// check that we have the templates available 
		await EnsureCanUseTemplates(version);

		if (!skipSolutionCreation)
		{
			// create the beam microservice project
			await RunDotnetCommand($"new beamservice -n \"{projectName}\" -o \"{projectPath}\"");

			// restore the microservice tools
			await RunDotnetCommand(
				$"tool restore --tool-manifest \"{Path.Combine(projectName, ".config", "dotnet-tools.json")}\"");
		}

		// add the microservice to the solution
		await RunDotnetCommand($"sln \"{solutionPath}\" add \"{projectPath}\"");

		string usedVersion = string.IsNullOrWhiteSpace(version) ? await GetVersion() : version;

		await UpdateProjectDependencyVersion(projectPath, "Beamable.Microservice.Runtime", usedVersion);

		// create the shared library project only if requested
		if (createCommonLibrary)
		{
			await RunDotnetCommand($"new beamlib -n \"{commonProjectName}\" -o \"{commonProjectPath}\"");

			// restore the shared library tools
			await RunDotnetCommand(
				$"tool restore --tool-manifest \"{Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}\"");

			// add the shared library to the solution
			await RunDotnetCommand($"sln \"{solutionPath}\" add \"{commonProjectPath}\"");

			// add the shared library as a reference of the project
			await RunDotnetCommand($"add \"{projectPath}\" reference \"{commonProjectPath}\"");

			await UpdateProjectDependencyVersion(commonProjectPath, "Beamable.Common", usedVersion);
		}

		return projectPath;
	}


	public async Task CreateCommon(ConfigService configService, string projectName, string dockerfilePath,
		string dockerBuildContextPath)
	{
		var commonProjectName = $"{projectName}Common";
		Log.Information("Docker file path is {DockerfilePath}", dockerfilePath);
		var serviceFolder = Path.GetDirectoryName(dockerfilePath);
		serviceFolder = configService.GetRelativePath(serviceFolder);
		Log.Information("Docker file folder is {DockerFileFolder}", serviceFolder);

		dockerfilePath = configService.GetRelativePath(Path.Combine(dockerBuildContextPath, dockerfilePath));
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
			await addUnityCommand.Handle(new AddUnityClientOutputCommandArgs { path = ".", Provider = provider });
		}

		// ask if we should link a Unreal project
		var addUnrealProject = AnsiConsole.Confirm(
			"Would you like to link an Unreal project? A linked Unreal project will receive autogenerated client updates.",
			true);
		if (addUnrealProject)
		{
			await addUnrealCommand.Handle(
				new AddUnrealClientOutputCommandArgs() { path = ".", Provider = provider });
				new UnrealAddProjectClientOutputCommandArgs() { path = ".", Provider = provider });
		}
	}

	private async Task<string> GetVersion()
	{
		var nugetPackages = (await _versionService.GetBeamableToolPackageVersions(replaceDashWithDot: false)).ToArray();

		return nugetPackages.Last().packageVersion;
	}

	static Task RunDotnetCommand(string arguments)
	{
		return CliExtensions.GetDotnetCommand(arguments).ExecuteAsyncAndLog().Task;
	}
}

public static class CliExtensions
{
	public static Command GetDotnetCommand(string dotnetPath, string arguments)
	{
		return Cli.Wrap(dotnetPath).WithEnvironmentVariables(new Dictionary<string, string> { ["DOTNET_CLI_UI_LANGUAGE"] = "en" }).WithArguments(arguments);
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
