using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace cli.Commands.Project;

public class RunProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();

	public bool forceRestart;
	public bool detach;
	public bool disableClientCodeGen;
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

		AddOption<bool>(new Option<bool>("--force", "With this flag, we restart any running services. Without it, we skip running services"), (args, b) => args.forceRestart = b);
		AddOption<bool>(new Option<bool>("--detach", "With this flag, the service will run the background after it has reached basic startup"), (args, b) => args.detach = b);
		AddOption<bool>(new Option<bool>("--no-client-gen", "We compile services that need compiling before running. This will disable the client-code generation part of the compilation"), (args, b) => args.disableClientCodeGen = b);

	}

	const float MIN_PROGRESS = .05f;
	void SendUpdate(string serviceId, string message = null, float progress = 0)
	{
		// remap the progress between MIN and 1.
		progress = MIN_PROGRESS + (progress * (1 - MIN_PROGRESS));
		this.SendResults<DefaultStreamResultChannel, RunProjectResultStream>(new RunProjectResultStream
		{
			serviceId = serviceId,
			message = message,
			progressRatio = progress
		});
	}

	public override async Task Handle(RunProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args,
			withTags: args.withServiceTags,
			withoutTags: args.withoutServiceTags,
			includeStorage: false,
			ref args.services);

		if (args.services.Count > 1)
		{
			Log.Warning("You are starting multiple services " +
						"Their log output will be shown interleaved; for optimal log viewing, use the `beam project logs' command");
		}

		// Emit some progress messages to let any listeners know we are doing work...
		foreach (var service in args.services)
		{
			SendUpdate(service, "discovering...", -MIN_PROGRESS * .9f);
		}

		// First, we need to find out which services are currently running.
		if (args.forceRestart)
		{
			Log.Verbose("starting discovery");
			await StopProjectCommand.DiscoverAndStopServices(args, new HashSet<string>(args.services), kill: true, TimeSpan.FromMilliseconds(100),
				evt =>
				{
					// do nothing?
				});
			Log.Verbose("finished discovery");
		}


		foreach (var service in args.services)
			SendUpdate(service, "resolving...", -MIN_PROGRESS * .7f);

		// Build out the list of services we'll actually want to start.
		var serviceTable = new Dictionary<string, HttpMicroserviceLocalProtocol>();
		foreach (var serviceName in args.services)
		{
			// If the service is not defined locally (as in, we can't run it locally for whatever reason)
			if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(serviceName, out var service))
			{
				throw new CliException($"No service definition available locally for service=[{serviceName}]");
			}

			serviceTable[serviceName] = service;
		}

		foreach (var service in args.services)
			SendUpdate(service, "starting...", 0);

		// For each of the filtered list of services, start a process that'll run it.
		var runTasks = new List<Task>();
		var failedTasks = new ConcurrentDictionary<string, RunProjectBuildErrorStream>();
		Log.Debug("Starting Services. SERVICES={Services}", string.Join(",", serviceTable.Keys));
		foreach ((string serviceName, HttpMicroserviceLocalProtocol service) in serviceTable)
		{
			var name = serviceName;

			var buildFlags = args.disableClientCodeGen ? ProjectService.BuildFlags.DisableClientCodeGen : ProjectService.BuildFlags.None;
			var runFlags = args.detach ? ProjectService.RunFlags.Detach : ProjectService.RunFlags.None;

			runTasks.Add(RunService(args, serviceName, new CancellationTokenSource(), buildFlags, runFlags, data =>
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
			}, (errorReport, exitCode) =>
			{
				var error = new RunProjectBuildErrorStream { serviceId = name, report = errorReport };
				failedTasks[name] = error;
				this.SendResults<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>(error);
			}, (progress, message) =>
			{
				SendUpdate(name, message, progress);
			}));
		}

		await Task.WhenAll(runTasks);

		if (failedTasks.Count > 0)
		{
			throw new CliException<RunFailErrorOutput>("failed to start all services")
			{
				payload = new RunFailErrorOutput { compilerErrors = failedTasks.Select(x => x.Value).ToList() }
			};
		}
	}

	public static async Task RunService(
		CommandArgs args,
		string serviceName,
		CancellationTokenSource serviceToken,
		ProjectService.BuildFlags buildFlags,
		ProjectService.RunFlags runFlags,
		Action<ProjectRunLogData> onLog = null,
		Action<ProjectErrorReport, int> onFailure = null,
		Action<float, string> onProgress = null)
	{
		var tokenSource =
			CancellationTokenSource.CreateLinkedTokenSource(args.Lifecycle.CancellationToken, serviceToken.Token);

		var currentProgress = 0f;
		var serviceLogProgressTable = new Dictionary<string, float>
		{
			[Beamable.Common.Constants.Features.Services.Logs.REGISTERING_STANDARD_SERVICES] = .42f,
			[Beamable.Common.Constants.Features.Services.Logs.REGISTERING_CUSTOM_SERVICES] = .45f,
			[Beamable.Common.Constants.Features.Services.Logs.SCANNING_CLIENT_PREFIX] = .5f,
			[Beamable.Common.Constants.Features.Services.Logs.SERVICE_PROVIDER_INITIALIZED] = .7f,
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

			var projectPath = args.ConfigService.BeamableRelativeToExecutionRelative(service.ProjectDirectory);
			Log.Debug("Found service definition, ctx=[{ServiceDockerBuildContextPath}] projectPath=[{ProjectPath}]",
				service.ProjectDirectory, projectPath);

			var logPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "serviceLogs",
				$"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
			Log.Debug($"service path=[{projectPath}]");

			var errorPath = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "buildLogs",
				$"{serviceName}.json");
			var errorPathDir = Path.GetDirectoryName(errorPath);
			Directory.CreateDirectory(errorPathDir);

			var exe = args.AppContext.DotnetPath;
			var commandStr = $"run --project {projectPath} --verbosity minimal -p:ErrorLog=\"{errorPath}%2Cversion=2\" -p:WarningLevel=0";

			if (buildFlags.HasFlag(ProjectService.BuildFlags.DisableClientCodeGen))
			{
				commandStr += " -p:GenerateClientCode=false";
			}

			if (runFlags.HasFlag(ProjectService.RunFlags.Detach) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// on windows, detaching "just works" (thought Chris, who wasn't on a windows machine)
				commandStr = $"sh -c \"{exe} {commandStr}\" &";
				exe = "nohup";
			}

			Log.Debug($"Running {exe} {commandStr}");
			var startInfo = new ProcessStartInfo(exe, commandStr)
			{
				Environment =
				{
					["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1",
					["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
					["LOG_PATH"] = logPath,
					["LOG_LEVEL"] = "verbose",
					["LOG_TYPE"] = "structured+file",
					["WATCH_TOKEN"] = "false",
					[Beamable.Common.Constants.EnvironmentVariables.BEAM_DOTNET_PATH] =
						args.AppContext.DotnetPath
				},
				WindowStyle = ProcessWindowStyle.Normal,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			};

			// startInfo.
			var proc = Process.Start(startInfo);
			var cts = new TaskCompletionSource();
			proc.Exited += (sender, eventArgs) =>
			{
				cts.TrySetResult();
			};
			proc.ErrorDataReceived += (sender, eventArgs) =>
			{
				var line = eventArgs.Data;
				if (line == null) return;
				onLog?.Invoke(new ProjectRunLogData
				{
					rawLogMessage = line,
					jsonData = null,
					forcedLogLevel = LogEventLevel.Error
				});
			};
			proc.OutputDataReceived += (sender, eventArgs) =>
			{
				var line = eventArgs.Data;
				if (line == null) return;
				// this may be structured JSON, or it could be a valid build message....
				try
				{
					var logData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(line,
						new JsonSerializerOptions { IncludeFields = true });


					var matches = serviceLogProgressTable.Where(kvp => line.Contains(kvp.Key));
					foreach (var kvp in matches)
					{
						if (currentProgress < kvp.Value)
						{
							currentProgress = kvp.Value;
							onProgress?.Invoke(kvp.Value, kvp.Key);
						}
						serviceLogProgressTable.Remove(kvp.Key);
					}


					onLog?.Invoke(new ProjectRunLogData { rawLogMessage = line, jsonData = logData });
				}
				catch
				{
					if (ErrorLike().Match(line).Success)
					{
						// this is a build failure message.
						onLog?.Invoke(new ProjectRunLogData
						{
							rawLogMessage = line,
							jsonData = null,
							forcedLogLevel = LogEventLevel.Error
						});
					}
					else
					{

						var matches = nonServiceLogProgressTable.Where(kvp => line.Contains(kvp.Key));
						foreach (var kvp in matches)
						{
							if (currentProgress < kvp.Value)
							{
								currentProgress = kvp.Value;
								onProgress?.Invoke(kvp.Value, kvp.Key);
							}
							nonServiceLogProgressTable.Remove(kvp.Key);
						}

						onLog?.Invoke(new ProjectRunLogData { rawLogMessage = line, jsonData = null });
					}
				}

			};
			proc.EnableRaisingEvents = true;
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();

			if (runFlags.HasFlag(ProjectService.RunFlags.Detach))
			{
				// wait for the progress to hit 1.
				while (!proc.HasExited && Math.Abs(currentProgress - 1) > .001f)
				{
					await Task.Delay(10);
				}
			}
			else
			{
				await cts.Task;
			}

			if (proc.HasExited && proc.ExitCode != 0)
			{
				var report = ProjectService.ReadErrorReport(errorPath);
				onFailure?.Invoke(report, proc.ExitCode);
			}
		}
		catch (Exception e)
		{
			Log.Error(e, e.Message);
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

		public LogEventLevel? forcedLogLevel;

		public LogEventLevel JsonLogLevel
		{
			get
			{
				if (!IsJson)
					return LogEventLevel.Fatal;
				if (!jsonData.TryGetValue("__l", out var logLevel))
					return LogEventLevel.Fatal;

				if (!LogUtil.TryParseLogLevel(logLevel.ToString(), out var level))
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


