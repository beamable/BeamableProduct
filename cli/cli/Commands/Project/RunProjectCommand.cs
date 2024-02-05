using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace cli.Commands.Project;


public class RunProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool watch;
}

public class RunProjectCommand : AppCommand<RunProjectCommandArgs>, IEmptyResult
{
	public RunProjectCommand() 
		: base("run", "Run a project")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.watch = i);
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(RunProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		var serviceTable = new Dictionary<string, HttpMicroserviceLocalProtocol>();
		var cancelTable = new Dictionary<string, CancellationTokenSource>();
		foreach (var serviceName in args.services)
		{
			if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(serviceName,
				    out var service))
			{
				throw new CliException($"No service definition available locally for service=[{serviceName}]");
			}

			cancelTable[serviceName] = new CancellationTokenSource();
			serviceTable[serviceName] = service;
		}
		var runTasks = new List<Task>();
		Log.Debug("starting services");

		foreach ((string serviceName, HttpMicroserviceLocalProtocol service) in serviceTable)
		{
			runTasks.Add(RunService(args, serviceName, service, cancelTable[serviceName]));
		}

		await Task.WhenAll(runTasks);
	}


	static async Task RunService(RunProjectCommandArgs args, string serviceName, HttpMicroserviceLocalProtocol service, CancellationTokenSource tokenSource)
	{
		
		Log.Debug($"Found service definition, ctx=[{service.DockerBuildContextPath}] dockerfile=[{service.RelativeDockerfilePath}]");
		var dockerfilePath = Path.Combine(args.ConfigService.GetRelativePath(service.DockerBuildContextPath), service.RelativeDockerfilePath);
		var projectPath = Path.GetDirectoryName(dockerfilePath);
		var logPath = Path.Combine(args.ConfigService.ConfigFilePath, "temp", "logs", $"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
		Log.Debug($"service path=[{projectPath}]");

		var watchPart = args.watch
			? $"watch --project {projectPath} run"
			: "run";
		var commandStr = $"{watchPart}";
	
		int killCounter = 1;

		var monitorTask = Task.Run(async () =>
		{
			while (!tokenSource.IsCancellationRequested)
			{
				await Task.Delay(50);
				if (killCounter == 0)
				{
					Log.Debug("stopping dotnet watch");
					tokenSource.Cancel();
				}
			}
		});

		var stdErrBuilder = new StringBuilder();
		await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, "--version")
			.WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Debug($"dotnet version: {x}")))
			.ExecuteAsync();
		Log.Debug($"Running {args.AppContext.DotnetPath} {commandStr}");
		var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithEnvironmentVariables(new Dictionary<string, string>
			{
				["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1",
				["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
				["LOG_PATH"] = logPath,
				["WATCH_TOKEN"] = "true"
			})
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				stdErrBuilder.Append(line);
				Console.Error.WriteLine("(watch error) " + line);
			}))
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				if (line.Trim() == "dotnet watch : Exited")
				{
					// check the kill counter in a *short* while to see if dotnet noticed a file change
					killCounter--;
				}

				if (line.Trim().StartsWith("dotnet watch : File changed:"))
				{
					// if we see this line, then we need to prevent the shutdown
					killCounter = 1;
				}
				Console.Error.WriteLine("(watch) " + line);
			}))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync(tokenSource.Token);


		var tailArgs = args.Create<TailLogsCommandArgs>();
		tailArgs.reconnect = true;
		tailArgs.service = new ServiceName(serviceName);

		var logCommand = ProjectLogsService.Handle(tailArgs, logMessage =>
		{
			var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
			Console.Error.WriteLine($"[{parsed.logLevel}] {parsed.message}");
			parsed.raw = logMessage;
		}, tokenSource.Token);

		try
		{
			await command;
			tokenSource.Cancel();
		}
		catch (OperationCanceledException)
		{
			Log.Debug("watch command was cancelled.");
		}

		await logCommand;
		await monitorTask;

	}
}
