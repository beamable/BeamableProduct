using cli.Services;
using CliWrap;
using System.CommandLine;
using Beamable.Server;
using microservice.Extensions;

namespace cli.Commands.Project;

public class OpenSolutionCommandArgs : CommandArgs, IHasSolutionFileArg
{
	public string SlnFilePath;

	public bool onlyGenerate;

	public bool useUnityFilter;
	
	public string SolutionFilePath
	{
		get => SlnFilePath;
		set => SlnFilePath = value;
	}
}

public class OpenSolutionCommand : AppCommand<OpenSolutionCommandArgs>, IEmptyResult
{
	public OpenSolutionCommand() : base("open", "Open the solution file for all services")
	{
	}

	public override void Configure()
	{
		SolutionCommandArgs.ConfigureSolutionFlag(this, _ => throw new CliException("Must have a valid .beamable folder"));
		AddOption(new Option<bool>("--only-generate", "Only generate the sln but do not open it"),
			(args, i) => args.onlyGenerate = i);
		
		AddOption(new Option<bool>("--from-unity", "Use a solution filter that hides projects that aren't writable in a Unity project"),
			(args, i) => args.useUnityFilter = i);
		
	}

	public static async Task CreateSolution(CommandArgs args, string slnPath)
	{
		var projService = args.ProjectService;

		var slnDir = Path.GetDirectoryName(slnPath);
		var slnFileName = Path.GetFileName(slnPath);
		
		if (!File.Exists(slnPath))
		{
			Log.Debug($"Creating solution file=[{slnPath}]");
			await projService.CreateNewSolution(slnDir, slnFileName);
		}
		
		var solutionPath = Path.Combine(args.ConfigService.WorkingDirectory, slnDir, slnFileName);
		var fullSolutionPath = Path.GetFullPath(solutionPath);
		Log.Debug($"Resolved sln path=[{solutionPath}]");
		
		
		foreach (var sd in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			if (!sd.IsLocal) continue;
			var proj = Path.GetFullPath(sd.AbsoluteProjectPath);
		
			Log.Debug($"adding project=[{proj}] to solution");
			var argStr = $"sln {solutionPath.EnquotePath()} add {proj.EnquotePath()}";
			await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, argStr)
				.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Error))
				.ExecuteAsyncAndLog();
		}

		var manifest = args.BeamoLocalSystem.BeamoManifest;
		foreach (var (beamoId, http) in manifest.HttpMicroserviceLocalProtocols)
		{
			if (!manifest.TryGetDefinition(beamoId, out var sd)) continue;
			if (!sd.IsLocal) continue;

			foreach (var unityDep in http.UnityAssemblyDefinitionProjectReferences)
			{
				await AddUnityDepToSolution(args, sd, fullSolutionPath, unityDep.Path);
			}
		}
		foreach (var (beamoId, db) in manifest.EmbeddedMongoDbLocalProtocols)
		{
			if (!manifest.TryGetDefinition(beamoId, out var sd)) continue;
			if (!sd.IsLocal) continue;

			foreach (var unityDep in db.UnityAssemblyDefinitionProjectReferences)
			{
				await AddUnityDepToSolution(args, sd, fullSolutionPath, unityDep.Path);
			}
		}
	}

	public static async Task<string> CreateSolutionFilter(CommandArgs args, string slnPath)
	{
		var projService = args.ProjectService;

		var badUnityPath = Path.Combine("Library", "PackageCache");
		var baseDir = args.ConfigService.BeamableWorkspace;
		return await projService.CreateSolutionFilterFile(slnPath, csProjPath =>
		{
			if (csProjPath.Contains(badUnityPath, StringComparison.InvariantCultureIgnoreCase))
			{
				// the project is part of a Unity cache; and should not be loaded. 
				//  TODO: its possible that the project is not ACTUALLY in a cache; 
				//  this folder detection is very low fidelity... 
				Log.Verbose($"Hiding project=[{csProjPath}] because it is in a unity package cache.");
				return false;
			}

			if (!csProjPath.StartsWith(baseDir))
			{
				// the project is not part of the .beamable folder; and should not be loaded. 
				//  TODO: again, this kind of stinks.
				Log.Verbose($"Hiding project=[{csProjPath}] because it is not in the root .beamable folder");
				return false;
			}
			
			return true;
		});
	}

	public override async Task Handle(OpenSolutionCommandArgs args)
	{
		await CreateSolution(args, args.SolutionFilePath);

		var openPath = args.SolutionFilePath;
		if (args.useUnityFilter)
		{
			var filterPath = await CreateSolutionFilter(args, args.SolutionFilePath);
			if (filterPath != null)
			{
				openPath = filterPath;
			}
		}
		
		if (args.onlyGenerate)
		{
			Log.Information("Not opening due to given option flag.");
		} else {
			Log.Information($"Opening solution {openPath}");
			
			// when opening visual studio, we need to clear the MSBUILD path, otherwise
			//  VS will try and use it, and get very confused and fail to open any of the
			//  projects.
			const string MSBUILD_EXE_PATH = "MSBUILD_EXE_PATH";
			var oldMsBuildExePath = Environment.GetEnvironmentVariable(MSBUILD_EXE_PATH);
			Environment.SetEnvironmentVariable(MSBUILD_EXE_PATH, null);
			var opener = args.DependencyProvider.GetService<IFileOpenerService>();
			await opener.OpenFileWithDefaultApp(Path.GetFullPath(openPath));
			Environment.SetEnvironmentVariable(MSBUILD_EXE_PATH, oldMsBuildExePath);

		}
	
		
	}
	
	static async Task AddUnityDepToSolution(CommandArgs args, BeamoServiceDefinition definition, string fullSlnPath, string projectPath){
		var slnProjPath = Path.Combine(definition.AbsoluteProjectDirectory, projectPath);
		var slnArgStr = $"sln {fullSlnPath.EnquotePath()} add {slnProjPath.EnquotePath()} -s \"UnityAssemblies (shared)\"";
		Log.Verbose($"adding assembly to solution, arg=[{slnArgStr}]");
		var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath,slnArgStr)
			.WithValidation(CommandResultValidation.None)
			.WithStandardErrorPipe(PipeTarget.ToDelegate(err =>
			{
				Log.Error($"Could not add unity ref to sln. err=[{err}]");
			}));
		await command.ExecuteAsync();
	}
	
}
