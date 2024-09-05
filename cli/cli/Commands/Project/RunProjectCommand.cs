using Beamable.Common.Api;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Text;

namespace cli.Commands.Project;

public class RunProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool detach;
	public bool forceRestart;
}

public class RunProjectCommand : AppCommand<RunProjectCommandArgs>, IEmptyResult
{
	public RunProjectCommand()
		: base("run", "Run a project")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (_, b) =>
		{
			if (b) throw new CliException("The --watch flag is no longer supported due to underlying .NET issues");
		});
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		AddOption<bool>(new Option<bool>("--force", "With this flag, we restart any running services. Without it, we skip running services"), (args, b) => args.detach = b);
		AddOption<bool>(new Option<bool>("--detach", "With this flag, we restart any running services. Without it, we skip running services"), (args, b) => args.forceRestart = b);
	}

	public override async Task Handle(RunProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		if (!args.detach && args.services.Count > 1)
		{
			Log.Warning("You are starting multiple services without the '--detach' flag. " +
			            "Their log output will be shown interleaved; for optimal log viewing, use '--detach' and then use 'beam project logs --ids <BeamoId>' for each service whose logs you wish to tail");
		}
		
		// First, we need to find out which services are currently running.
		var runningServices = new Dictionary<string, ServiceDiscoveryEvent>();
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();
		await foreach (var evt in discovery.StartDiscovery(args, TimeSpan.FromMilliseconds(Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS * 2)))
		{
			// we don't care about this service, because we aren't stopping it.
			if (!args.services.Contains(evt.service)) continue;

			// We need to wait for when discovery emits the event with the health-port set up (meaning, they are running).
			if (!runningServices.ContainsKey(evt.service) && evt.healthPort != 0)
			{
				runningServices[evt.service] = evt;
				if (runningServices.Keys.Count == args.services.Count)
				{
					// we've found all the required services...
					break;
				}
			}
		}

		// Stop listening for discovery events.
		await discovery.Stop();

		// Build out the list of services we'll actually want to start.
		var serviceTable = new Dictionary<string, HttpMicroserviceLocalProtocol>();
		var cancelTable = new Dictionary<string, CancellationTokenSource>();
		foreach (var serviceName in args.services)
		{
			// If the service is not defined locally (as in, we can't run it locally for whatever reason)
			if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(serviceName, out var service))
			{
				throw new CliException($"No service definition available locally for service=[{serviceName}]");
			}

			// If the service is already running...
			if (runningServices.TryGetValue(serviceName, out var runningServiceEvt))
			{
				// If we are not forcing restarts of running services, we skip this service and log it.
				if (!args.forceRestart)
				{
					Log.Information("Service is already running. If you want to restart it automatically, use '--force'. SERVICE_NAME={ServiceName}", serviceName);
					continue;
				}

				// If we are forcing a restart, we stop the service and then start it up again.
				var beamoLocalSystem = args.BeamoLocalSystem;
				await StopProjectCommand.StopRunningService(runningServiceEvt, beamoLocalSystem, serviceName, _ => { });
			}

			// If the service is not running here, we add it to the list of services we want to start.
			cancelTable[serviceName] = new CancellationTokenSource();
			serviceTable[serviceName] = service;
		}

		// For each of the filtered list of services, start a process that'll run it.
		var runTasks = new List<Task>();
		Log.Debug("Starting Services. SERVICES={Services}", string.Join(",", serviceTable.Keys));
		foreach ((string serviceName, HttpMicroserviceLocalProtocol service) in serviceTable)
		{
			runTasks.Add(RunService(args, serviceName, !args.detach, cancelTable[serviceName]));
		}
		
		await Task.WhenAll(runTasks);
	}


	public static async Task RunService(CommandArgs args, string serviceName, bool watchProcess, CancellationTokenSource serviceToken)
	{
		var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(args.Lifecycle.CancellationToken, serviceToken.Token);

		// Setup a thread to run the server process.
		// This runs the currently built MS .dll via `dotnet` and keeps a handle to the resulting process.
		// If it dies, this command exits with a non-zero error code.
		var serverTask = Task.Run(async () =>
		{
			try
			{
				var service = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x => x.BeamoId == serviceName);
				if (service == null)
					throw new CliException($"service does not exist, service=[{serviceName}]");

				var projectPath = args.ConfigService.BeamableRelativeToExecutionRelative(service.ProjectDirectory);
				Log.Debug("Found service definition, ctx=[{ServiceDockerBuildContextPath}] projectPath=[{ProjectPath}]", service.ProjectDirectory, projectPath);

				var logPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "logs", $"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
				Log.Debug($"service path=[{projectPath}]");

				var commandStr = $"run --project {projectPath}";
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
						["WATCH_TOKEN"] = "false",
						[Beamable.Common.Constants.EnvironmentVariables.BEAM_DOTNET_PATH] = args.AppContext.DotnetPath,
					})
					.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
					{
						Console.Error.WriteLine("(watch error) " + line);
					}))
					.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
					{
						Console.Error.WriteLine("(watch) " + line);
					}))
					.WithValidation(CommandResultValidation.None)
					.ExecuteAsync(tokenSource.Token);

				var res = await command;
				tokenSource.Cancel();

				if (res.ExitCode != 0)
					throw new CliException($"Failed to run service with id: [{service.BeamoId}]. You can check the logs at {logPath}.", res.ExitCode, true);
			}
			catch (Exception e)
			{
				Log.Error(e, e.Message);
			}
		}, tokenSource.Token);

		// Setup the tail logs command so that we can keep track of the logs of the running service while it boots up.
		var tailArgs = args.Create<TailLogsCommandArgs>();
		tailArgs.reconnect = true;
		tailArgs.service = new ServiceName(serviceName);

		var serviceStartedLogsReceived = false;
		var logCommand = ProjectLogsService.Handle(tailArgs, logMessage =>
		{
			Console.Error.WriteLine($"[{logMessage.logLevel}] {logMessage.message}");
			if (logMessage.message.Contains("Service ready for traffic."))
				serviceStartedLogsReceived = true;
		}, tokenSource.Token);

		var serviceStarted = false;
		var healthCheckTask = Task.Run(async () =>
		{
			var route = $"https://dev.api.beamable.com/basic/{args.AppContext.Cid}.{args.AppContext.Pid}.{serviceName}/admin/HealthCheck";
			while (true)
			{
				try
				{
					await args.Requester.CustomRequest<object>(Method.GET, route,
						customHeaders: new[] { $"{Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY}={ServiceRoutingStrategyExtensions.GetRoutingKeyMap(new[] { serviceName })}" });
					serviceStarted = true;
					break;
				}
				catch (Exception)
				{
					await Task.Delay(50);
				}
			}
		}, tokenSource.Token);

		while (!serviceStarted || !serviceStartedLogsReceived)
		{
			await Task.Delay(50, tokenSource.Token);
		}

		if (watchProcess)
		{
			await healthCheckTask;
			await serverTask;
			await logCommand;
		}
	}
}
