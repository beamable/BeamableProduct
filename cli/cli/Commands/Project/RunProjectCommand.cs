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


	static async Task RunService(RunProjectCommandArgs args, string serviceName, HttpMicroserviceLocalProtocol protocol, CancellationTokenSource tokenSource)
	{

		var service = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x => x.BeamoId == serviceName);
		if (service == null)
		{
			throw new CliException($"service does not exist, service=[{serviceName}]");
		}

		var projectPath = args.ConfigService.BeamableRelativeToExecutionRelative(service.ProjectDirectory);
		Log.Debug("Found service definition, ctx=[{ServiceDockerBuildContextPath}] projectPath=[{ProjectPath}]", service.ProjectDirectory, projectPath);

		var logPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "logs", $"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
		Log.Debug($"service path=[{projectPath}]");

		var watchPart = args.watch
			? $"watch --project {projectPath} run"
			: $"run --project {projectPath}";
		var commandStr = $"{watchPart}";

		/* dotnet watch has odd semantics around process exiting...
		 *  when the watch-command needs to restart the app, it will kill the old
		 *  process and start a new one, causing the "Exited" log to print.
		 *  However, if we kill the process remotely, we get the exact same
		 *  "Exited" log line, making it hard to know if the process exited
		 *  because the watch-command wanted it to, or if we actually wanted it to
		 *  die.
		 *
		 *  Because of this, we'll keep a task running watching for the existence
		 *  of the SECOND dotnetwatch log, about a file changing. When that log
		 *  is detected within a short duration after the Exited log line, then
		 *  it is safe to assume we DO NOT want to shut the process down. 
		 */
		var keepProcessAlive = true;
		var monitorTask = Task.Run(async () =>
		{
			while (!tokenSource.IsCancellationRequested)
			{
				await Task.Delay(50);
				if (keepProcessAlive == false)
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
					// check in a *short* while to see if dotnet noticed a file change
					keepProcessAlive = false;
				}

				if (line.Trim().StartsWith("dotnet watch : File changed:"))
				{
					// if we see this line, then we need to prevent the shutdown
					keepProcessAlive = true;
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
			Console.Error.WriteLine($"[{logMessage.logLevel}] {logMessage.message}");
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
