using cli.Services;
using CliWrap;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
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

	public override async Task Handle(OpenSolutionCommandArgs args)
	{
		var projService = args.ProjectService;

		if (!args.GetSlnExists())
		{
			Log.Debug($"Creating solution file=[{args.SolutionFilePath}]");
			await projService.CreateNewSolution(args.GetSlnDirectory(), args.GetSlnFileName());
		}
		
		var solutionPath = Path.Combine(args.ConfigService.WorkingDirectory, args.GetSlnDirectory(), Path.GetFileName(args.SlnFilePath));
		var fullSolutionPath = Path.GetFullPath(solutionPath);
		Log.Debug($"Resolved sln path=[{solutionPath}]");
		
		
		foreach (var sd in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			if (!sd.IsLocal) continue;
			var proj = Path.GetFullPath(sd.AbsoluteProjectPath);
		
			Log.Debug($"adding project=[{proj}] to solution");
			var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"sln {solutionPath.EnquotePath()} add {proj.EnquotePath()}");
			command.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Error));
			await command.ExecuteAsync();
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

		if (args.onlyGenerate)
		{
			Log.Information("Not opening due to given option flag.");
		} else {
			Log.Information($"Opening solution {solutionPath}");
			await OpenFileWithDefaultApp(solutionPath);
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
	
	public static async Task OpenFileWithDefaultApp(string filePath)
	{
		try
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = filePath,
					UseShellExecute = true // Important for launching with default app
				}
			};
			var success = process.Start();
			await Task.Delay(500);
			Log.Information("Opened : " + success);
		}
		catch (Exception ex)
		{
			Log.Error("Failed to open program: " +ex.Message);
		}
	}
}
