using Beamable.Common.Api;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using System.CommandLine;
using System.Text;
using System.Text.Json;

namespace cli.Commands.Project;

public class RunProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool detach;
	public bool forceRestart;
}

public class RunProjectResultStream
{
	/// <summary>
	/// for which service is this progress update for?
	/// </summary>
	public string serviceId;
	
	/// <summary>
	/// a description of the progress
	/// </summary>
	public string message;
	
	/// <summary>
	/// between 0 and 1, where 1 means the service is fully initialized and ready for traffic
	/// </summary>
	public float progressRatio;
}

public class RunProjectCommand : AppCommand<RunProjectCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, RunProjectResultStream>
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
		AddOption<bool>(new Option<bool>("--force", "With this flag, we restart any running services. Without it, we skip running services"), (args, b) => args.forceRestart = b);

		var detachOption = new Option<bool>("--detach",
			"With this flag, we don't remain attached to the running service process");
		detachOption.AddAlias("-d");
		AddOption<bool>(detachOption, (args, b) => args.detach = b);
	}

	const float MIN_PROGRESS = .05f;
	void SendUpdate(string serviceId, string message=null, float progress=0)
	{
		// remap the progress between MIN and 1.
		progress = MIN_PROGRESS + (progress * (1 - MIN_PROGRESS));
		this.SendResults(new RunProjectResultStream
		{
			serviceId = serviceId,
			message = message,
			progressRatio = progress
		});
	}
	
	public override async Task Handle(RunProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		if (!args.detach && args.services.Count > 1)
		{
			Log.Warning("You are starting multiple services without the '--detach' flag. " +
			            "Their log output will be shown interleaved; for optimal log viewing, use '--detach' and then use 'beam project logs --ids <BeamoId>' for each service whose logs you wish to tail");
		}
		
		// Emit some progress messages to let any listeners know we are doing work...
		foreach (var service in args.services)
		{
			SendUpdate(service, "discovering...", -MIN_PROGRESS * .9f);
		}
		
		// First, we need to find out which services are currently running.
		var runningServices = new Dictionary<string, ServiceDiscoveryEvent>();
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();
		Log.Verbose("starting discovery");
		await foreach (var evt in discovery.StartDiscovery(
			               args: args, 
			               timeout: TimeSpan.FromMilliseconds(Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS), 
			               mode: DiscoveryMode.LOCAL))
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
		Log.Verbose("finished discovery");

		await discovery.Stop();
		Log.Verbose("stopped discovery");

		
		foreach (var service in args.services)
			SendUpdate(service, "resolving...", -MIN_PROGRESS * .7f);
		
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
				SendUpdate(serviceName, "stopping...", -MIN_PROGRESS * .6f);
				await StopProjectCommand.StopRunningService(runningServiceEvt, beamoLocalSystem, serviceName, _ => { });
				SendUpdate(serviceName, "stopping...", -MIN_PROGRESS * .5f);
			}

			// If the service is not running here, we add it to the list of services we want to start.
			cancelTable[serviceName] = new CancellationTokenSource();
			serviceTable[serviceName] = service;
		}
		
		foreach (var service in args.services)
			SendUpdate(service, "starting...", 0);

		// For each of the filtered list of services, start a process that'll run it.
		var runTasks = new List<Task>();
		Log.Debug("Starting Services. SERVICES={Services}", string.Join(",", serviceTable.Keys));
		foreach ((string serviceName, HttpMicroserviceLocalProtocol service) in serviceTable)
		{
			runTasks.Add(RunService(args, serviceName, !args.detach, cancelTable[serviceName], data =>
			{
				if (data.IsJson)
				{
					Log.Write(data.JsonLogLevel, data.JsonLogMessage);
				}
				else
				{
					Log.Information(data.rawLogMessage);
				}
			}));
		}
		
		await Task.WhenAll(runTasks);
	}


	public static async Task RunService(CommandArgs args, string serviceName, bool watchProcess, CancellationTokenSource serviceToken, Action<ProjectRunLogData> onLog=null)
	{
		var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(args.Lifecycle.CancellationToken, serviceToken.Token);
		var serviceStartedLogsReceived = false;

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
						["LOG_TYPE"] = "structured+file",
						["WATCH_TOKEN"] = "false",
						[Beamable.Common.Constants.EnvironmentVariables.BEAM_DOTNET_PATH] = args.AppContext.DotnetPath,
					})
					.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
					{
						Console.Error.WriteLine("(watch error) " + line);
					}))
					.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
					{
						// this may be structured JSON, or it could be a valid build message....
						try
						{
							var logData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(line, new JsonSerializerOptions
							{
								IncludeFields = true
							});

							onLog?.Invoke(new ProjectRunLogData
							{
								rawLogMessage = line,
								jsonData = logData
							});
						}
						catch
						{
							onLog?.Invoke(new ProjectRunLogData
							{
								rawLogMessage = line,
								jsonData = null
							});
						}

						if (line.Contains(Beamable.Common.Constants.Features.Services.Logs.READY_FOR_TRAFFIC_PREFIX))
						{
							serviceStartedLogsReceived = true;
						}

						// Console.Error.WriteLine("(watch) " + line);
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
		
		while (!serviceStartedLogsReceived)
		{
			await Task.Delay(50, tokenSource.Token);
		}

		if (watchProcess)
		{
			await serverTask;
		}
		else
		{
			Log.Information($"Detaching from running service=[{serviceName}]");
		}
	}
	
	public class StructuredMicroserviceLog
	{
		[JsonProperty("__t")]
		public string timeStamp;

		[JsonProperty("__m")]
		public string message;

		[JsonProperty("__l")]
		public string logLevel;

		[JsonProperty("__x")]
		public string exception;
	}

	public class ProjectRunLogData
	{
		public string rawLogMessage;
		public Dictionary<string, object> jsonData;

		public bool IsJson => jsonData != null;

		public LogEventLevel JsonLogLevel
		{
			get
			{
				if (!IsJson) 
					return LogEventLevel.Fatal;
				if (!jsonData.TryGetValue("__l", out var logLevel))
					return LogEventLevel.Fatal;
				
				if (!Enum.TryParse<LogEventLevel>(logLevel.ToString(), ignoreCase: true, out var level))
					return LogEventLevel.Fatal;

				return level;
			}
		}

		public string JsonLogMessage
		{
			get
			{
				if (!IsJson) return rawLogMessage;
				if (!jsonData.TryGetValue("__m", out var message))
					return rawLogMessage;
				return message.ToString();
			}
		}
	}
}


