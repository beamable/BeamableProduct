using cli.Services;
using CliWrap;
using Serilog;
using System.Diagnostics;

namespace cli.Commands.Project;

public class OpenSolutionCommandArgs : CommandArgs, IHasSolutionFileArg
{
	public string SlnFilePath;

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
		Log.Debug($"Resolved sln path=[{solutionPath}]");
		
		
		foreach (var sd in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			if (!sd.IsLocal) continue;
			var proj = Path.GetFullPath(sd.AbsoluteProjectPath);
		
			Log.Debug($"adding project=[{proj}] to solution");
			var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"sln {solutionPath} add {proj}");
			command.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Error));
			await command.ExecuteAsync();
		}
		
		Log.Information($"Opening solution {solutionPath}");
		await OpenFileWithDefaultApp(solutionPath);
		
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
