using cli.Services;
using CliWrap;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using Beamable.Common.Dependencies;
using microservice.Extensions;

namespace cli.Commands.Project;

public class OpenSolutionCommandArgs : CommandArgs, IHasSolutionFileArg
{
	public string SlnFilePath;

	public bool onlyGenerate;

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
		
		var solutionPath = Path.Combine(args.ConfigService.WorkingDirectory, slnDir, slnPath);
		var fullSolutionPath = Path.GetFullPath(solutionPath);
		Log.Debug($"Resolved sln path=[{solutionPath}]");
		
		
		foreach (var sd in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			if (!sd.IsLocal) continue;
			var proj = Path.GetFullPath(sd.AbsoluteProjectPath);
		
			Log.Debug($"adding project=[{proj}] to solution");
			var argStr = $"sln {solutionPath.EnquotePath()} add {proj.EnquotePath()}";
			var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, argStr);
			command.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Error));
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

	public override async Task Handle(OpenSolutionCommandArgs args)
	{
		await CreateSolution(args, args.SolutionFilePath);
		
		if (args.onlyGenerate)
		{
			Log.Information("Not opening due to given option flag.");
		} else {
			Log.Information($"Opening solution {args.SolutionFilePath}");
			
			// this await exists to try and allow the sln file to close all hooks
			//  on windows, with visual studio, it will fail to open the FIRST time
			//  after the file is generated. But if you pass the --only-generate flag
			//  and open it by hand, it works... Puzzling...
			await Task.Delay(TimeSpan.FromMilliseconds(500));
			var opener = args.DependencyProvider.GetService<IFileOpenerService>();
			await opener.OpenFileWithDefaultApp(args.SolutionFilePath);
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
