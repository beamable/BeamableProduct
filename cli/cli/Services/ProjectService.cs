using Beamable.Common.Semantics;
using cli.Commands.Project;
using cli.Dotnet;
using cli.Unreal;
using CliWrap;
using Microsoft.CodeAnalysis.Sarif;
using Spectre.Console;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Beamable.Server;
using microservice.Extensions;
using System.Diagnostics;
using System.Xml.Linq;

namespace cli.Services;

public class ProjectService
{
	private const string UNINSTALL_COMMAND = "new --uninstall";
	private readonly ConfigService _configService;
	private readonly VersionService _versionService;
	private readonly IAppContext _app;

	private EngineProjectData _engineProjects;

	public bool? ConfigFileExists { get; }

	public ProjectService(ConfigService configService, VersionService versionService, IAppContext app)
	{
		_configService = configService;
		_versionService = versionService;
		_app = app;
		ConfigFileExists = _configService.DirectoryExists;
		_engineProjects = configService.GetLinkedEngineProjects();
	}

	public List<string> GetLinkedUnityProjects()
	{
		return _engineProjects.unityProjectsPaths.Select(u => u.Path).ToList();
	}

	public List<EngineProjectData.Unreal> GetLinkedUnrealProjects()
	{
		return _engineProjects.unrealProjectsPaths.ToList();
	}

	public void AddUnityProject(string relativePath)
	{
		_engineProjects.unityProjectsPaths.Add(new EngineProjectData.Unity(){Path = relativePath});
		_configService.SetLinkedEngineProjects(_engineProjects);
	}

	public void AddUnrealProject(string projectPath, string microservicePluginName, string microservicePluginNameBp)
	{
		// Always ensure that we store things relative to the root of the UE project (not the repo)
		var unrealRootPath = EnsureUnrealRootPath(projectPath);

		// Find beamable folder (it must exist either as a parent of the UE project root OR inside the UE project root folder.
		var beamableFolderPath = FindBeamableFolderPath(unrealRootPath);

		// The path must always be stored relative to the .beamable folder as we run commands always through that.
		projectPath = Path.GetRelativePath(beamableFolderPath.ToString(), unrealRootPath.ToString());
		projectPath = projectPath.StartsWith(".") ? projectPath.Substring(1) : projectPath;
		projectPath = projectPath.StartsWith("/") ? projectPath.Substring(1) : projectPath;

		var projData = new EngineProjectData.Unreal() { };

		var pathToBackendGenerationJson = $"Plugins/BeamableCore/Source/{UnrealSourceGenerator.currentGenerationPassDataFilePath}.json";
		projData.Path = projectPath;
		projData.SourceFilesPath = projectPath + $"Plugins/{microservicePluginName}/";

		projData.CoreProjectName = microservicePluginName;
		projData.BlueprintNodesProjectName = microservicePluginNameBp;

		// These are defined relative to the SourceFilesPath
		projData.MsCoreHeaderPath = "Source/" + projData.CoreProjectName + "/Public/";
		projData.MsCoreCppPath = "Source/" + projData.CoreProjectName + "/Private/";
		projData.MsBlueprintNodesHeaderPath = "Source/" + projData.BlueprintNodesProjectName + "/Public/";
		projData.MsBlueprintNodesCppPath = "Source/" + projData.BlueprintNodesProjectName + "/Private/";

		projData.BeamableBackendGenerationPassFile = projectPath + pathToBackendGenerationJson;

		_engineProjects.unrealProjectsPaths.Add(projData);
		_configService.SetLinkedEngineProjects(_engineProjects);
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


	public async Task EnsureCanUseTemplates(string version)
	{
		var info = await GetTemplateInfo();

		if (!info.HasTemplates ||
		    !string.Equals(version, info.templateVersion, StringComparison.CurrentCultureIgnoreCase))
		{
			await PromptAndInstallTemplates(info.templateVersion, version, true);
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

		var command = $"new install {packageName}::{version}";
		Log.Verbose($"installing templates as {command}");
		var (result, installStream) = await CliExtensions.RunWithOutput(_app.DotnetPath, command);
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
		public string SolutionDirectory
		{
			get
			{
				string path = Path.GetDirectoryName(SolutionPath);
				return string.IsNullOrEmpty(path) ? "." : path;
			}
		}

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
		var path = _configService.GetRelativeToBeamableWorkspacePath(Path.Combine(rootServicesPath, beamId));
		return path;
	}

	public async Task<NewServiceInfo> CreateNewPortalExtension(NewPortalExtensionCommandArgs args)
	{
		string usedVersion = VersionService.GetNugetPackagesForExecutingCliVersion().ToString();
		var portalExtensionInfo = new NewServiceInfo();

		// check that we have the templates available
		await EnsureCanUseTemplates(usedVersion);

		var outputPath = Path.Combine(_configService.BaseDirectory, "extensions");

		portalExtensionInfo.ServicePath = Path.Combine(outputPath, args.ProjectName);
		await RunDotnetCommand($"new portalextensionapp -n {args.ProjectName} -o {portalExtensionInfo.ServicePath.EnquotePath()}");

		// Probably need this for finding all apps
		//await args.BeamoLocalSystem.InitManifest();

		return portalExtensionInfo;
	}

	public async Task<NewServiceInfo> CreateNewStorage(NewStorageCommandArgs args)
	{
		string usedVersion = VersionService.GetNugetPackagesForExecutingCliVersion().ToString();
		var microserviceInfo = new NewServiceInfo();
		// check that we have the templates available
		await EnsureCanUseTemplates(usedVersion);
		microserviceInfo.SolutionPath = args.SlnFilePath;
		if (!args.GetSlnExists())
		{
			microserviceInfo.SolutionPath = await CreateNewSolution(args.GetSlnDirectory(), args.GetSlnFileName());
		}

		if (!_configService.IsInBeamableWorkspace(microserviceInfo.SolutionPath))
		{
			throw new CliException(
				$"Solution file({microserviceInfo.SolutionPath}) should not exists outside beamable directory({_configService.ConfigDirectoryPath}) or its subdirectories.");
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
		await RunDotnetCommand($"new beamstorage -n {args.ProjectName} -o {microserviceInfo.ServicePath.EnquotePath()} --no-update-check --TargetFrameworkOverride {args.targetFramework}");
		await RunDotnetCommand($"sln {microserviceInfo.SolutionPath.EnquotePath()} add {microserviceInfo.ServicePath.EnquotePath()}");

		await args.BeamoLocalSystem.InitManifest();

		return microserviceInfo;
	}

	public async Task<NewServiceInfo> CreateNewMicroservice(NewMicroserviceArgs args)
	{
		// check that we have the templates available
		string usedVersion = VersionService.GetNugetPackagesForExecutingCliVersion().ToString();
		await EnsureCanUseTemplates(usedVersion);

		var microserviceInfo = new NewServiceInfo { SolutionPath = args.SlnFilePath };
		if (!args.GetSlnExists())
		{
			await CreateNewSolution(args.GetSlnDirectory(), args.GetSlnFileName());
		}

		if (string.IsNullOrWhiteSpace(args.ServicesBaseFolderPath))
		{
			var directory = Path.GetDirectoryName(microserviceInfo.SolutionPath);
			args.ServicesBaseFolderPath = Path.Combine(directory!, "services");
		}

		if (!_configService.IsInBeamableWorkspace(microserviceInfo.SolutionPath))
		{
			throw new CliException(
				$"Solution file({microserviceInfo.SolutionPath}) should not exists outside beamable directory({_configService.ConfigDirectoryPath}) or its subdirectories.");
		}

		microserviceInfo.ServicePath = await CreateNewService(microserviceInfo.SolutionPath, args.ProjectName, args.ServicesBaseFolderPath, usedVersion, args.GenerateCommon, args.TargetFramework);
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
		var slnNameWithExtension = solutionName;
		if (!slnNameWithExtension.EndsWith(".sln"))
		{
			slnNameWithExtension += ".sln";
		}

		var slnNameWithoutExtension = slnNameWithExtension.Substring(0, slnNameWithExtension.Length - ".sln".Length);
		
		await RunDotnetCommand($"new sln -n {slnNameWithoutExtension.EnquotePath()} -o {solutionPath.EnquotePath()} --format sln", out var buffer);

		return Path.Combine(solutionPath, slnNameWithExtension);
	}

	public delegate bool ProjectFilterPredicate(string fullCsProjPath);
	
	public async Task<string> CreateSolutionFilterFile(string slnPath, ProjectFilterPredicate filter)
	{
		{ // make sure the path actually has the sln file extension
			if (!slnPath.EndsWith(".sln"))
			{
				slnPath += ".sln";
			}
		}
		
		var slnFilterPath = Path.ChangeExtension(slnPath, ".writable.slnf");
		var slnDir = Path.GetDirectoryName(slnPath);

		var slnListArg = $"sln {slnPath.EnquotePath()} list";
		var lines = new List<string>();
		var command = CliExtensions.GetDotnetCommand(_app.DotnetPath, slnListArg)
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
		{
			lines.Add(line);
		}));
		// command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(buffer));
		var result = await command.ExecuteAsync();
		if (result.ExitCode != 0)
		{
			throw new CliException("Failed to run sln list");
		}

		var bufferStr = string.Join(Environment.NewLine, lines);
		{ // do some validation that the output matches our expectations... 
			string error = null;
			if (lines.Count == 0)
				error = ($"Unspected sln filter. Expected header. output=[{bufferStr}]");
			if (lines[0] != "Project(s)")
				error = ($"Unspected sln filter. Expected 'Project(s)'. output=[{bufferStr}]");
			if (lines.Count > 1 && lines[1].Any(c => c != '-'))
				error = ($"Unspected sln filter. Expected a line of dashes. output=[{bufferStr}]");

			if (!string.IsNullOrEmpty(error))
			{
				Log.Error(error);
				return null;
			}
		}
		
		
		var filteredProjectPaths = new List<string>();
		for (var i = 2; i < lines.Count; i++)
		{
			var relativePathToSln = lines[i];
			var fullPath = Path.GetFullPath(Path.Combine(slnDir, relativePathToSln));
			if (filter(fullPath))
			{
				filteredProjectPaths.Add(relativePathToSln);
			}
		}

		var model = new SolutionFilterModel
		{
			solution = new SolutionFilterModel.Solution
			{
				// this file lives right next to the .sln, 
				//  so the relative path should be just the file name
				path = Path.GetFileName(slnPath), 
				projects = filteredProjectPaths
			}
		};
		var filterJson = JsonSerializer.Serialize(model, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
		File.WriteAllText(slnFilterPath, filterJson);
		return slnFilterPath;
	}

	[Serializable]
	public class SolutionFilterModel
	{
		public Solution solution;
		[Serializable]
		public class Solution
		{
			public string path;
			public List<string> projects = new List<string>();
		}
		// {
		// 	"solution": {
		// 		"path": "BeamableServices.sln",
		// 		"projects": [
		// 		"services\\CommonLand\\CommonLand.csproj",
		// 		"services\\Serv1\\Serv1.csproj",
		// 		"services\\Serv2\\Serv2.csproj",
		// 		"services\\Serv3\\Serv3.csproj"
		// 			]
		// 	}
		// }
	}

	public async Task<string> CreateNewService(string solutionPath, string projectName, string rootServicesPath, string version, bool generateCommon, string targetFramework)
	{
		if (!File.Exists(solutionPath))
		{
			throw new CliException($"{solutionPath} does not exist");
		}

		var projectPath = Path.Combine(rootServicesPath, projectName);

		// create the beam microservice project
		await RunDotnetCommand($"new beamservice -n {projectName.EnquotePath()} -o {projectPath.EnquotePath()} --no-update-check --TargetFrameworkOverride {targetFramework}");

		// restore the microservice tools
		await RunDotnetCommand(
			$"tool restore --tool-manifest \"{Path.Combine(projectName, ".config", "dotnet-tools.json")}\"");

		// add the microservice to the solution
		await RunDotnetCommand($"sln {solutionPath.EnquotePath()} add {projectPath.EnquotePath()}");
		
		// If we are linked to an Unreal project, we should add the property to turn on UE-Specific static analyses.
		if ((_engineProjects?.unrealProjectsPaths?.Count ?? 0) > 0)
		{
			var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
			var doc = XDocument.Load(csprojPath);
			
			var propertyGroup = doc.Descendants("PropertyGroup").FirstOrDefault(e => (e.Attribute("Label")?.Value ?? "") == "Beamable Settings");
			Debug.Assert(propertyGroup != null, nameof(propertyGroup) + " != null");
			propertyGroup.Add(new XElement("EnableUnrealBlueprintCompatibility", "true"));
			doc.Save(csprojPath);
			
			// At the moment, we look for <Project Sdk="Microsoft.NET.Sdk"> to be the first line of any Beamable-compatible csprojs.
			// Saving with XDocument adds <?xml version="1.0" encoding="utf-8"?> to the top of the file.
			// We need to remove this line that the XML library adds.
			// TODO: In general, we need a more robust way of identifying projects we care about BEFORE feeding the file into MsBuild
			//  See -> ProjectContextUtil.FindCsharpProjects
			string[] lines = File.ReadAllLines(csprojPath);
			File.WriteAllLines(csprojPath, lines.Skip(1).ToArray());
		}
			
		// create the shared library project only if requested
		if (generateCommon)
		{
			var commonProjectName = $"{projectName}Common";
			var commonProjectPath = Path.Combine(rootServicesPath, commonProjectName);
			await CreateCommonProject(commonProjectName, commonProjectPath, version, solutionPath);
			// add the shared library as a reference of the project
			await RunDotnetCommand($"add {projectPath.EnquotePath()} reference {commonProjectPath.EnquotePath()}");
		}

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
		await RunDotnetCommand($"new beamlib -n {commonProjectName.EnquotePath()} -o {commonProjectPath.EnquotePath()}");

		// restore the shared library tools
		await RunDotnetCommand($"tool restore --tool-manifest \"{Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}\"");

		// add the shared library to the solution
		await RunDotnetCommand($"sln {solutionPath.EnquotePath()} add {commonProjectPath.EnquotePath()}");
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
		var versionToUpdate = string.IsNullOrWhiteSpace(version)
			? string.Empty
			: $" --version \"{version}\"";

		return RunDotnetCommand($"add {projectPath.EnquotePath()} package {packageName}{versionToUpdate}");
	}

	public Task RunDotnetCommand(string arguments)
	{
		return CliExtensions.GetDotnetCommand(_app.DotnetPath, arguments).ExecuteAsyncAndLog().Task;
	}
	public Task RunDotnetCommand(string arguments, out StringBuilder buffer)
	{
		return CliExtensions.GetDotnetCommand(_app.DotnetPath, arguments).ExecuteAsyncAndLog(out buffer).Task;
	}

	
	[System.Flags]
	public enum BuildFlags
	{
		None,
		DisableClientCodeGen
	}
	
	
	[System.Flags]
	public enum RunFlags
	{
		None,
		Detach
	}
	
	public static async Task WatchBuild(BuildProjectCommandArgs args, ServiceName serviceName, BuildFlags buildFlags, Action<ProjectErrorReport> onReport)
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
		var dockerfilePath = service.AbsoluteDockerfilePath;
		var projectPath = Path.GetDirectoryName(dockerfilePath);
		var commandStr = $"build {projectPath.EnquotePath()} -v n -p:ErrorLog=\"{errorPath}%2Cversion=2\"";
		Log.Debug($"dotnet command=[{args.AppContext.DotnetPath} {commandStr}]");

		if (buildFlags.HasFlag(BuildFlags.DisableClientCodeGen))
		{
			commandStr += " -p:GenerateClientCode=false";
		}
		
		using var cts = new CancellationTokenSource();

		var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithEnvironmentVariables(new Dictionary<string, string> { ["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1", ["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1", })
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				Log.Information(line);
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				Log.Error(line);
			}))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync(cts.Token);


		var res = await command;

		var exitCode = res.ExitCode;
		if (exitCode != 0)
		{
			Log.Error($"Failed to build command=[{args.AppContext.DotnetPath} {commandStr}]");
		}

		var report = ReadErrorReport(errorPath);
		onReport?.Invoke(report);
	}

	public static ProjectErrorReport ReadErrorReport(string errorLogPath)
	{
		Log.Debug("Reading SARIF report at " + errorLogPath);
		try
		{
			var outputs = new List<ProjectErrorResult>();
			SarifLog log = SarifLog.Load(errorLogPath);
			foreach (var result in log.Results())
			{
				if (result.Level is FailureLevel.Note or FailureLevel.None or FailureLevel.Warning) continue;
				var location = result.Locations?.FirstOrDefault();

				outputs.Add(new ProjectErrorResult
				{
					level = result.Level.ToString(),
					formattedMessage = result.FormatForVisualStudio(),
					uri = location?.PhysicalLocation?.ArtifactLocation?.Uri?.ToString() ?? "",
					line = location?.PhysicalLocation?.Region?.StartLine ?? 0,
					column = location?.PhysicalLocation?.Region?.StartColumn ?? 0
				});
			}

			return new ProjectErrorReport { errors = outputs, isSuccess = outputs.Count == 0 };
		}
		catch (Exception ex)
		{
			Log.Error($"Failed to read SARIF report. type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}] ");
			throw new CliException($"Failed to read SARIF report. type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}] ", 2, false);
		}
	}
}

[Serializable]
public class ProjectErrorReport
{
	public bool isSuccess;
	public List<ProjectErrorResult> errors = new List<ProjectErrorResult>();
}

[Serializable]
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
	public static async Task<(CommandResult, StringBuilder)> RunWithOutput(string dotnetPath, string arguments, string workingDirectory = "")
	{
		var builder = new StringBuilder();
		var cmd = GetDotnetCommand(dotnetPath, arguments)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(builder))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(builder));

		if (!string.IsNullOrWhiteSpace(workingDirectory))
		{
			cmd = cmd.WithWorkingDirectory(workingDirectory);
		}
		
		var result = await cmd.ExecuteAsync();

		return (result, builder);
	}

	public static Command GetDotnetCommand(string dotnetPath, string arguments)
	{
		var command = Cli.Wrap(dotnetPath)
			.WithEnvironmentVariables(new Dictionary<string, string> { ["DOTNET_CLI_UI_LANGUAGE"] = "en" })
			.WithArguments(arguments)
			;
		return command;
	}

	public static CommandTask<CommandResult> ExecuteAsyncAndLog(this Command command)
	{
		Log.Information($"Running '{command.TargetFilePath} {command.Arguments}'");

		var  buffer = new StringBuilder();
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

	public static CommandTask<CommandResult> ExecuteAsyncAndLog(this Command command, out StringBuilder buffer)
	{
		Log.Information($"Running '{command.TargetFilePath} {command.Arguments}'");

		buffer = new StringBuilder();
		var commandTask = command
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(buffer))
			.ExecuteAsync();
		return commandTask;
	}
}
