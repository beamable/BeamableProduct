using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using cli.Dotnet;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using microservice.Extensions;
using Beamable.Server;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace cli.Commands.Project;

public class RunProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();
	
	public bool forceRestart;
	public bool detach;
	public bool disableClientCodeGen;
	public int requireProcessId;
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

[Serializable]
public class RunProjectBuildErrorStream  
{
	public string serviceId;
	public ProjectErrorReport report;
}

public class RunProjectBuildErrorStreamChannel : IResultChannel
{
	public string ChannelName => "buildErrors";
}

public class RunFailErrorOutput : ErrorOutput
{
	public List<RunProjectBuildErrorStream> compilerErrors = new List<RunProjectBuildErrorStream>();
}

public partial class RunProjectCommand : AppCommand<RunProjectCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, RunProjectResultStream>
	, IResultSteam<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>
	, IReportException<RunFailErrorOutput>
{
	[GeneratedRegex(": error CS(\\d\\d\\d\\d):")]
	public static partial Regex ErrorLike();

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
		ProjectCommand.AddServiceTagsOption(this,
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);

		AddOption<bool>(
			new Option<bool>("--force",
				"With this flag, we restart any running services. Without it, we skip running services"),
			(args, b) => args.forceRestart = b);
		AddOption<bool>(
			new Option<bool>("--detach",
				"With this flag, the service will run the background after it has reached basic startup"),
			(args, b) => args.detach = b);
		AddOption<bool>(
			new Option<bool>("--no-client-gen",
				"We compile services that need compiling before running. This will disable the client-code generation part of the compilation"),
			(args, b) => args.disableClientCodeGen = b);

		AddOption(
			new Option<int>("--require-process-id",
				$"Forwards the given process-id to the {Beamable.Common.Constants.EnvironmentVariables.BEAM_REQUIRE_PROCESS_ID} environment variable of the running Microservice. The Microservice will self-destruct if the given process exits"),
			(args, i) => args.requireProcessId = i);
	}

	const float MIN_PROGRESS = .05f;

	/// <summary>
	/// Spacing between service starts within a single run. Mirrors
	/// <c>LocalStackUpCommand.GroupLaunchStagger</c>, which spaces the individual <c>--ids</c> steps
	/// but cannot reach inside a <c>--with-group</c> run because that is one process fanning out
	/// internally.
	/// </summary>
	static readonly TimeSpan GroupFanOutStagger = TimeSpan.FromMilliseconds(2000);

	void SendUpdate(string serviceId, string message = null, float progress = 0)
	{
		// remap the progress between MIN and 1.
		progress = MIN_PROGRESS + (progress * (1 - MIN_PROGRESS));
		this.SendResults<DefaultStreamResultChannel, RunProjectResultStream>(new RunProjectResultStream
		{
			serviceId = serviceId, message = message, progressRatio = progress
		});
	}

	public override async Task Handle(RunProjectCommandArgs args)
	{
		// A SECRET in the environment `beam project run` was started from is the user's secret override. Read it
		// before any service starts, so it is never confused with a value something set on this process later.
		var secretOverride = Environment.GetEnvironmentVariable(RunScopeIdentity.ENV_SECRET);

		ProjectCommand.FinalizeServicesArg(args,
			withTags: args.withServiceTags,
			withoutTags: args.withoutServiceTags,
			includeStorage: false,
			ref args.services);

		if (args.services.Count > 1)
		{
			Log.Warning("You are starting multiple services" +
			            " Their log output will be shown interleaved; for optimal log viewing, use the `beam project logs' command");
		}

		// Emit some progress messages to let any listeners know we are doing work...
		foreach (var service in args.services)
		{
			SendUpdate(service, "discovering...", -MIN_PROGRESS * .9f);
		}

		// First, we need to find out which services are currently running.
		if (args.forceRestart)
		{
			Log.Trace("starting discovery");
			await StopProjectCommand.DiscoverAndStopServices(args, new HashSet<string>(args.services),
				"project run command", kill: true, TimeSpan.FromMilliseconds(100),
				evt =>
				{
					// do nothing?
				});
			Log.Trace("finished discovery");
		}


		foreach (var service in args.services)
			SendUpdate(service, "resolving...", -MIN_PROGRESS * .7f);

		BeamoLocalManifest beamoLocalManifest = args.BeamoLocalSystem.BeamoManifest;
		Dictionary<string, BeamoServiceDefinition> serviceDefinitions =
			beamoLocalManifest.ServiceDefinitions.ToDictionary(def => def.BeamoId, def => def);

		// Build out the list of services we'll actually want to start.
		var serviceTable = new Dictionary<string, BeamoServiceDefinition>();
		foreach (var serviceName in args.services)
		{
			// If the service is not defined locally (as in, we can't run it locally for whatever reason)

			if (!serviceDefinitions.TryGetValue(serviceName, out var serviceDefinition))
			{
				throw new CliException($"No service definition available locally for service=[{serviceName}]");
			}

			serviceTable.Add(serviceName, serviceDefinition);
		}

		// Decide which scope (realm pid, or zone) and secret each service authenticates with, and validate a SECRET
		// override against the scopes being run, before anything builds. Each service gets its identity explicitly
		// (per-process env for microservices, its own args object for in-process portal extensions), so one
		// service's scope can never leak into another's.
		var identities = await RunScopeIdentity.Resolve(args, serviceTable.Values, secretOverride);

		// Make sure every storage the selected services need is running before any of them starts, and hand each
		// service the resolved connection strings. Left to each service's own `generate-env --auto-deploy`, services
		// sharing a storage would race to create the same container.
		var storageFailures = new Dictionary<string, string>();
		var storageEnvByService = await EnsureRequiredStorages(args, serviceTable, storageFailures);

		foreach (var service in args.services)
		{
			if (!storageFailures.ContainsKey(service))
			{
				SendUpdate(service, "starting...", 0);
			}
		}

		// For each of the filtered list of services, start a process that'll run it.
		var runTasks = new List<Task>();
		var failedTasks = new ConcurrentDictionary<string, RunProjectBuildErrorStream>();
		Log.Debug("Starting Services. SERVICES={Services}", string.Join(",", serviceTable.Keys));

		// A fault on the PortalExtension/EmbeddedMongoDb branches used to sit unobserved in runTasks:
		// Task.WhenAll below cannot surface it while sibling tasks are effectively infinite
		// (RunForever / ExitedTask), so the service hung at its last progress message with nothing in
		// stderr and nothing on any channel. The HttpMicroservice branch is already guarded inside
		// RunService; this gives the other two the same treatment, and emits a TERMINAL stream message
		// so a consumer tracking the stream channel stops waiting.
		async Task Guarded(string serviceId, Task task)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				Log.Error($"Service [{serviceId}] failed to start: {ex.Message}");
				SendUpdate(serviceId, $"failed: {ex.Message}", 1);
			}
		}

		var startedCount = 0;
		foreach ((string serviceName, BeamoServiceDefinition serviceDef) in serviceTable)
		{
			var name = serviceName;

			// Storages were already started by EnsureRequiredStorages, and services whose storage failed to start
			// already received their terminal failure update.
			if (serviceDef.Protocol == BeamoProtocolType.EmbeddedMongoDb || storageFailures.ContainsKey(name))
			{
				continue;
			}

			// Stagger starts. Launching N services in the same tick is what trips the gateway's rate
			// limiter during each service's generate-env (every one of them calls accounts/admin/me).
			// `beam local up` already spaces its individual --ids steps by GroupLaunchStagger; a
			// --with-group run is a single process that fans out internally, so it needs its own.
			if (startedCount > 0)
			{
				await Task.Delay(GroupFanOutStagger);
			}

			startedCount++;

			var buildFlags = args.disableClientCodeGen
				? ProjectService.BuildFlags.DisableClientCodeGen
				: ProjectService.BuildFlags.None;
			var runFlags = args.detach ? ProjectService.RunFlags.Detach : ProjectService.RunFlags.None;

			switch (serviceDef.Protocol)
			{
				case BeamoProtocolType.HttpMicroservice:
				{
					var serviceEnv = identities[name].ToProcessEnvironment();
					foreach ((string key, string value) in storageEnvByService.GetValueOrDefault(name) ?? new Dictionary<string, string>())
					{
						serviceEnv[key] = value;
					}

					runTasks.Add(RunService(args, serviceName, new CancellationTokenSource(), buildFlags, runFlags,
						OnLogReceived, (errorReport, exitCode) =>
						{
							var error = new RunProjectBuildErrorStream { serviceId = name, report = errorReport };
							failedTasks[name] = error;
							this.SendResults<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>(error);
						}, (progress, message) =>
						{
							SendUpdate(name, message, progress);
						}, serviceEnv));
					break;
				}
				case BeamoProtocolType.PortalExtension:
				{
					var cToken = new CancellationTokenSource();
					var portalExtensionConfig = args.ConfigService.LoadPortalExtensionConfig();

					if (runFlags.HasFlag(ProjectService.RunFlags.Detach))
					{
						// Start a new beam subprocess wrapped in cmd/nohup
						runTasks.Add(Guarded(name, RunPortalExtensionDetached(args, name,
							(progress, message) => SendUpdate(name, message, progress), OnLogReceived)));
					}
					else
					{
						// If a process ID is passed we need to watch if it is exited to kill the portal extension execution.
						if (args.requireProcessId > 0)
						{
							_ = Task.Run(async () =>
							{
								try
								{
									var parentProc = Process.GetProcessById(args.requireProcessId);
									await parentProc.WaitForExitAsync(cToken.Token);
								}
								catch
								{
									Log.Warning(
										$"Could not find Process with ID {args.requireProcessId}, treating as exited process and closing terminating portal extension.");
								}

								Log.Debug("Parent process exited; terminating portal extension process.");
								// TODO: Find a way to stop the portal extension without killing the CLI Server 
								Environment.Exit(0);
							}, cToken.Token);
						}

						var beamActivity = args.Provider.GetService<BeamActivity>();
						// Resolve the portal base url here (where CommandArgs is in scope) so the
						// "open in browser" landing URL honors the --portal-url override. This is the
						// same resolver the portal command and remote-config service use.
						var portalBaseUrl = cli.Portal.PortalCommand.GetPortalBaseUrl(args);
						runTasks.Add(Guarded(name, args.BeamoLocalSystem.RunLocalPortalExtension(
							serviceDef, args.BeamoLocalSystem, portalExtensionConfig, args.AppContext, beamActivity,
							identities[name],
							portalBaseUrl: portalBaseUrl,
							onProgress: (progress, message) => SendUpdate(name, message, progress),
							token: cToken.Token)));
					}

					break;
				}
			}
		}

		await Task.WhenAll(runTasks);

		if (failedTasks.Count > 0)
		{
			throw new CliException<RunFailErrorOutput>("failed to start all services")
			{
				payload = new RunFailErrorOutput { compilerErrors = failedTasks.Select(x => x.Value).ToList() }
			};
		}

		if (storageFailures.Count > 0)
		{
			throw new CliException("failed to start all services: " +
			                       string.Join("; ", storageFailures.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
		}
	}

	/// <summary>
	/// Works out which storages the services in <paramref name="serviceTable"/> need (their dependencies, plus any
	/// storage passed directly), checks them all against Docker at once, and starts the missing ones in parallel.
	/// Returns, per microservice, the env vars carrying its storages' connection strings (see
	/// <see cref="BeamoLocalSystem.ENV_PRERESOLVED_STORAGE_CONNSTR_PREFIX"/>). Every requested service that cannot
	/// start because of a storage failure is added to <paramref name="storageFailures"/> and sent a terminal update.
	/// </summary>
	async Task<Dictionary<string, Dictionary<string, string>>> EnsureRequiredStorages(RunProjectCommandArgs args,
		Dictionary<string, BeamoServiceDefinition> serviceTable, Dictionary<string, string> storageFailures)
	{
		var envByService = new Dictionary<string, Dictionary<string, string>>();

		// storage id -> the requested services that depend on it
		var storageDependents = new Dictionary<string, List<string>>();
		foreach ((string serviceName, BeamoServiceDefinition serviceDef) in serviceTable)
		{
			if (serviceDef.Protocol == BeamoProtocolType.EmbeddedMongoDb)
			{
				storageDependents.TryAdd(serviceName, new List<string>());
				continue;
			}

			if (serviceDef.Protocol != BeamoProtocolType.HttpMicroservice)
			{
				continue;
			}

			foreach (var dependency in args.BeamoLocalSystem.GetDependencies(serviceName))
			{
				if (!storageDependents.TryGetValue(dependency.name, out var dependents))
				{
					dependents = new List<string>();
					storageDependents[dependency.name] = dependents;
				}

				dependents.Add(serviceName);
			}
		}

		if (storageDependents.Count == 0)
		{
			return envByService;
		}

		if (!await args.BeamoLocalSystem.CheckIsRunning())
		{
			throw CliExceptions.DOCKER_NOT_RUNNING;
		}

		foreach (var dependents in storageDependents.Values)
		{
			foreach (var service in dependents)
			{
				SendUpdate(service, "checking storages...", -MIN_PROGRESS * .5f);
			}
		}

		Log.Debug("Ensuring storages are running. STORAGES={Storages}", string.Join(",", storageDependents.Keys));
		var resolutions = await args.BeamoLocalSystem.EnsureLocalStorages(storageDependents.Keys,
			onUpdate: (storageId, message, progress) =>
			{
				// Only storages the caller asked for by id are tracked on the stream.
				if (serviceTable.ContainsKey(storageId))
				{
					SendUpdate(storageId, message, progress);
				}
			},
			token: args.Lifecycle.CancellationToken);

		foreach ((string storageId, LocalStorageResolution resolution) in resolutions)
		{
			if (!resolution.Succeeded)
			{
				var reason = $"storage=[{storageId}] failed to start: {resolution.Error.Message}";
				Log.Error(reason);
				if (serviceTable.ContainsKey(storageId))
				{
					storageFailures[storageId] = reason;
					SendUpdate(storageId, $"failed: {reason}", 1);
				}

				foreach (var service in storageDependents[storageId])
				{
					storageFailures[service] = reason;
					SendUpdate(service, $"failed: {reason}", 1);
				}

				continue;
			}

			foreach (var service in storageDependents[storageId])
			{
				if (!envByService.TryGetValue(service, out var env))
				{
					env = new Dictionary<string, string>();
					envByService[service] = env;
				}

				env[BeamoLocalSystem.ENV_PRERESOLVED_STORAGE_CONNSTR_PREFIX + storageId] = resolution.ConnectionString;
			}
		}

		return envByService;
	}

	private void OnLogReceived(ProjectRunLogData data)
	{
		if (data.IsJson)
		{
			Log.Write(data.JsonLogLevel, data.JsonLogMessage);
		}
		else if (data.forcedLogLevel.HasValue)
		{
			Log.Write(data.forcedLogLevel.Value, data.rawLogMessage);
		}
		else
		{
			Log.Information(data.rawLogMessage);
		}
	}

	/// <summary>
	/// Advances the progress milestones matched by <paramref name="line"/>, across every table.
	/// <para>Milestones arrive on both transports: MSBuild emits plain text ("Bundling Beamable
	/// Properties..."), while the runtime's own milestones ("Starting Prepare") go through the
	/// structured logger as JSON — <c>RunService</c> forces <c>LOG_TYPE=structured+file</c>. Consulting
	/// only one table per branch made the "Starting Prepare" (.3) milestone unreachable, so a service
	/// that died between .2 and .42 — i.e. during <c>generate-env</c> — looked frozen at "Bundling
	/// Beamable Properties...". Both tables are checked on both paths.</para>
	/// </summary>
	private static void ApplyProgress(
		string line,
		float[] currentProgress,
		Action<float, string> onProgress,
		params Dictionary<string, float>[] tables)
	{
		foreach (var table in tables)
		{
			var matches = table.Where(kvp => line.Contains(kvp.Key)).ToList();
			foreach (var kvp in matches)
			{
				if (currentProgress[0] < kvp.Value)
				{
					currentProgress[0] = kvp.Value;
					onProgress?.Invoke(kvp.Value, kvp.Key);
				}

				table.Remove(kvp.Key);
			}
		}
	}

	/// <summary>
	/// Handles a single stdout line from a running service process.
	/// Shared by <see cref="RunService"/> and <see cref="RunPortalExtensionDetached"/>.
	/// </summary>
	private static void HandleOutputLine(
		string line,
		float[] currentProgress,
		Dictionary<string, float> serviceLogProgressTable,
		Dictionary<string, float> nonServiceLogProgressTable,
		Action<float, string> onProgress,
		Action<ProjectRunLogData> onLog)
	{
		if (line == null) return;
		try
		{
			var logData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(line,
				new JsonSerializerOptions { IncludeFields = true });

			ApplyProgress(line, currentProgress, onProgress, serviceLogProgressTable, nonServiceLogProgressTable);

			onLog?.Invoke(new ProjectRunLogData { rawLogMessage = line, jsonData = logData });
		}
		catch
		{
			if (ErrorLike().Match(line).Success)
			{
				onLog?.Invoke(new ProjectRunLogData
				{
					rawLogMessage = line, jsonData = null, forcedLogLevel = LogLevel.Error
				});
			}
			else
			{
				ApplyProgress(line, currentProgress, onProgress, serviceLogProgressTable, nonServiceLogProgressTable);

				onLog?.Invoke(new ProjectRunLogData { rawLogMessage = line, jsonData = null });
			}
		}
	}

	/// <summary>
	/// Handles a single stderr line from a running service process.
	/// Shared by <see cref="RunService"/> and <see cref="RunPortalExtensionDetached"/>.
	/// </summary>
	private static void HandleErrorLine(string line, Action<ProjectRunLogData> onLog)
	{
		if (line == null) return;
		onLog?.Invoke(new ProjectRunLogData { rawLogMessage = line, jsonData = null, forcedLogLevel = LogLevel.Error });
	}

	public static async Task RunService(
		RunProjectCommandArgs args,
		string serviceName,
		CancellationTokenSource serviceToken,
		ProjectService.BuildFlags buildFlags,
		ProjectService.RunFlags runFlags,
		Action<ProjectRunLogData> onLog = null,
		Action<ProjectErrorReport, int> onFailure = null,
		Action<float, string> onProgress = null,
		IReadOnlyDictionary<string, string> additionalEnvVars = null)
	{
		var tokenSource =
			CancellationTokenSource.CreateLinkedTokenSource(args.Lifecycle.CancellationToken, serviceToken.Token);

		// float[] so the value is mutable inside the lambda closures below.
		var currentProgress = new float[] { 0f };
		var serviceLogProgressTable = new Dictionary<string, float>
		{
			[Beamable.Common.Constants.Features.Services.Logs.REGISTERING_STANDARD_SERVICES] = .42f,
			[Beamable.Common.Constants.Features.Services.Logs.REGISTERING_CUSTOM_SERVICES] = .45f,
			[Beamable.Common.Constants.Features.Services.Logs.SCANNING_CLIENT_PREFIX] = .5f,
			[Beamable.Common.Constants.Features.Services.Logs.SERVICE_PROVIDER_INITIALIZED] = .7f,
			// Between provider init and the event provider — that is the order ProvideService runs them in.
			// Without a milestone here, the window where federation registration happens (and can fail) was
			// indistinguishable from a service simply being slow to reach "ready".
			[Beamable.Common.Constants.Features.Services.Logs.FEDERATION_REGISTERED] = .72f,
			[Beamable.Common.Constants.Features.Services.Logs.EVENT_PROVIDER_INITIALIZED] = .75f,
			[Beamable.Common.Constants.Features.Services.Logs.READY_FOR_TRAFFIC_PREFIX] = 1
		};
		var nonServiceLogProgressTable = new Dictionary<string, float>
		{
			["Determining projects to restore..."] = .1f,
			["Bundling Beamable Properties..."] = .2f,
			["Starting Prepare"] = .3f,
		};

		// Setup a thread to run the server process.
		// This runs the currently built MS .dll via `dotnet` and keeps a handle to the resulting process.
		// If it dies, this command exits with a non-zero error code.
		try
		{
			var service =
				args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x => x.BeamoId == serviceName);
			if (service == null)
				throw new CliException($"service does not exist, service=[{serviceName}]");

			var projectPath = service.AbsoluteProjectDirectory;
			Log.Debug("Found service definition, projectPath=[{ProjectPath}]", projectPath);

			var logPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "serviceLogs",
				$"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
			Log.Debug($"service path=[{projectPath}]");

			var errorPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "buildLogs",
				$"{serviceName}.json");
			var errorPathDir = Path.GetDirectoryName(errorPath);
			Directory.CreateDirectory(errorPathDir);

			var exe = args.AppContext.DotnetPath;
			var commandStr =
				$"run --project {projectPath.EnquotePath()} --verbosity minimal -p:ErrorLog=\"{errorPath}%2Cversion=2\" -p:WarningLevel=0";

			if (buildFlags.HasFlag(ProjectService.BuildFlags.DisableClientCodeGen))
			{
				commandStr += " -p:GenerateClientCode=false";
			}

			var isDetach = runFlags.HasFlag(ProjectService.RunFlags.Detach);

			Log.Debug($"Running {exe} {commandStr}");

			var envVars = new Dictionary<string, string>
			{
				["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1",
				["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
				["LOG_PATH"] = logPath,
				["LOG_LEVEL"] = "verbose",
				["LOG_TYPE"] = "structured+file",
				["WATCH_TOKEN"] = "false",
				// A truthy BEAM_SKIP_LOCAL_ENV inherited from whatever launched the CLI would make the service
				// skip its own `generate-env`, the step that injects STORAGE_CONNSTR_<storage> (a zone
				// microservice would then fail at runtime with "Connection string for storage '<X>' is null or
				// empty"). Removing it guarantees each child resolves its env via generate-env, exactly as a
				// `--ids` run does. Its scope and secret are set explicitly through additionalEnvVars.
				["BEAM_SKIP_LOCAL_ENV"] = null,
				[Beamable.Common.Constants.EnvironmentVariables.BEAM_DOTNET_PATH] = args.AppContext.DotnetPath
			};
			if (args.requireProcessId > 0)
			{
				envVars[Beamable.Common.Constants.EnvironmentVariables.BEAM_REQUIRE_PROCESS_ID] =
					args.requireProcessId.ToString();
			}

			if (additionalEnvVars != null)
			{
				foreach ((string key, string value) in additionalEnvVars)
				{
					envVars[key] = value;
				}
			}

			var handle = StartProcessUtil.Run(
				exe, commandStr,
				isDetach: isDetach,
				environmentVariables: envVars,
				onStdout: line =>
					HandleOutputLine(line, currentProgress, serviceLogProgressTable, nonServiceLogProgressTable,
						onProgress, onLog),
				onStderr: line =>
					HandleErrorLine(line, onLog));

			var proc = handle.Process;
			var shouldAutoKill = false;

			if (isDetach)
			{
				// wait for the progress to hit 1.
				while (!proc.HasExited && Math.Abs(currentProgress[0] - 1) > .001f && !args.Lifecycle.IsCancelled)
				{
					await Task.Delay(10);
				}

				if (args.Lifecycle.IsCancelled)
				{
					shouldAutoKill = true;
				}
			}
			else
			{
				try
				{
					await handle.ExitedTask.WaitAsync(args.Lifecycle.CancellationToken);
				}
				catch (TaskCanceledException)
				{
					shouldAutoKill = true;

				}
			}

			if (shouldAutoKill)
			{
				try
				{
					Log.Trace("Killing sub process microservice.");
					proc.Kill(true);
				}
				catch (Exception ex)
				{
					// does not matter.
					Log.Trace($"failed to kill microservice type=[{ex.GetType().Name}] message=[{ex.Message}]");
				}
			}
			else if (proc.HasExited && proc.ExitCode != 0)
			{
				var report = ProjectService.ReadErrorReport(errorPath);
				onFailure?.Invoke(report, proc.ExitCode);
			}
		}
		catch (Exception e)
		{
			Log.Error(e.Message);
		}
	}

	/// <summary>
	/// Runs a PortalExtension service as a detached subprocess by spawning a new beam process in cmd or sh depending on the OS
	/// </summary>
	public static async Task RunPortalExtensionDetached(
		RunProjectCommandArgs args,
		string serviceName,
		Action<float, string> onProgress = null,
		Action<ProjectRunLogData> onLog = null)
	{
		// Determine how to invoke beam in the detached subprocess.
		// - Production: if IsDotnetHost is true, we use dotnet beam to call the command.
		// - Development: if IsDotnetHost is false, we use the 'BeamableProduct\cli\cli\bin\Debug\net10.0\Beamable.Tools.exe' to run without needing to publish the local dotnet tool.
		var processPath = Environment.ProcessPath ?? string.Empty;
		var isDotnetHost = processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
		                   || processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);

		var innerExe = isDotnetHost ? args.AppContext.DotnetPath : processPath;
		var innerArgs = isDotnetHost
			? $"beam project run --ids {serviceName}"
			: $"project run --ids {serviceName}";

		// Forward the parent-process watchdog to the inner beam process.
		if (args.requireProcessId > 0)
		{
			innerArgs += $" --require-process-id {args.requireProcessId}";
		}

		if (args.forceRestart)
		{
			innerArgs += $" --force {args.forceRestart}";
		}

		// The command will be wrapped for a detached process tree (cmd.exe on Windows, nohup on Linux/macOS) by StartAsync.
		Log.Debug($"Running detached portal extension: {innerExe} {innerArgs}");

		var readyOrExited = new TaskCompletionSource();

		var currentProgress = new float[] { 0f };
		var serviceLogProgressTable = new Dictionary<string, float>
		{
			[Beamable.Common.Constants.Features.Services.Logs.PORTAL_EXTENSION_RUNNING] = 1
		};
		var nonServiceLogProgressTable = new Dictionary<string, float>();

		var handle = StartProcessUtil.Run(
			innerExe, innerArgs,
			isDetach: true,
			onStdout: line =>
				HandleOutputLine(line, currentProgress, serviceLogProgressTable, nonServiceLogProgressTable,
					(progress, message) =>
					{
						onProgress?.Invoke(progress, message);
						if (Math.Abs(progress - 1f) < .001f)
							readyOrExited.TrySetResult();
					},
					onLog),
			onStderr: line =>
				HandleErrorLine(line, onLog));

		// Also complete when the process exits (even if progress never reached 1).
		_ = handle.ExitedTask.ContinueWith(_ => readyOrExited.TrySetResult());

		var shouldAutoKill = false;
		try
		{
			await readyOrExited.Task.WaitAsync(args.Lifecycle.CancellationToken);
		}
		catch (TaskCanceledException)
		{
			shouldAutoKill = true;
		}

		if (shouldAutoKill)
		{
			try
			{
				Log.Debug("Killing detached portal extension process.");
				handle.Process.Kill(true);
			}
			catch (Exception ex)
			{
				Log.Trace($"failed to kill portal extension type=[{ex.GetType().Name}] message=[{ex.Message}]");
			}
		}
	}

	public class StructuredMicroserviceLog
	{
		[JsonProperty("__t")] public string timeStamp;

		[JsonProperty("__m")] public string message;

		[JsonProperty("__l")] public string logLevel;

		[JsonProperty("__x")] public string exception;
	}

	public class ProjectRunLogData
	{
		public string rawLogMessage;
		public Dictionary<string, object> jsonData;

		public bool IsJson => jsonData != null;

		public LogLevel? forcedLogLevel;

		public LogLevel JsonLogLevel
		{
			get
			{
				if (!IsJson)
					return LogLevel.Critical;
				if (!jsonData.TryGetValue("__l", out var logLevel))
					return LogLevel.Critical;

				if (!LogUtil.TryParseSystemLogLevel(logLevel.ToString(), out var level))
					return LogLevel.Critical;

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

