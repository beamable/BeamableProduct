using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Dotnet;
using cli.Services;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using microservice.Extensions;
using Beamable.Server;
using Microsoft.Extensions.Logging;

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
		
		AddOption<bool>(new Option<bool>("--force", "With this flag, we restart any running services. Without it, we skip running services"), (args, b) => args.forceRestart = b);
		AddOption<bool>(new Option<bool>("--detach", "With this flag, the service will run the background after it has reached basic startup"), (args, b) => args.detach = b);
		AddOption<bool>(new Option<bool>("--no-client-gen", "We compile services that need compiling before running. This will disable the client-code generation part of the compilation"), (args, b) => args.disableClientCodeGen = b);

		AddOption(
			new Option<int>("--require-process-id",
				$"Forwards the given process-id to the {Beamable.Common.Constants.EnvironmentVariables.BEAM_REQUIRE_PROCESS_ID} environment variable of the running Microservice. The Microservice will self-destruct if the given process exits"),
			(args, i) => args.requireProcessId = i);
	}

	const float MIN_PROGRESS = .05f;
	void SendUpdate(string serviceId, string message=null, float progress=0)
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
			await StopProjectCommand.DiscoverAndStopServices(args, new HashSet<string>(args.services), "project run command", kill: true, TimeSpan.FromMilliseconds(100),
				evt =>
				{
					// do nothing?
				});
			Log.Verbose("finished discovery");
		}
		
		
		foreach (var service in args.services)
			SendUpdate(service, "resolving...", -MIN_PROGRESS * .7f);

		BeamoLocalManifest beamoLocalManifest = args.BeamoLocalSystem.BeamoManifest;
		Dictionary<string, BeamoServiceDefinition> serviceDefinitions = beamoLocalManifest.ServiceDefinitions.ToDictionary(def => def.BeamoId, def => def);
		
		// Build out the list of services we'll actually want to start.
		var serviceTable = new Dictionary<string, BeamoServiceDefinition>();
		foreach (var serviceName in args.services)
		{
			// If the service is not defined locally (as in, we can't run it locally for whatever reason)
			
			if (!serviceDefinitions.TryGetValue(serviceName, out var serviceDefinition ))
			{
				throw new CliException($"No service definition available locally for service=[{serviceName}]");
			}

			serviceTable.Add(serviceName, serviceDefinition);
		}
		
		foreach (var service in args.services)
			SendUpdate(service, "starting...", 0);

		// For each of the filtered list of services, start a process that'll run it.
		var runTasks = new List<Task>();
		var failedTasks = new ConcurrentDictionary<string, RunProjectBuildErrorStream>();
		Log.Debug("Starting Services. SERVICES={Services}", string.Join(",", serviceTable.Keys));
		foreach ((string serviceName, BeamoServiceDefinition serviceDef) in serviceTable)
		{
			var name = serviceName;

			var buildFlags = args.disableClientCodeGen ? ProjectService.BuildFlags.DisableClientCodeGen : ProjectService.BuildFlags.None;
			var runFlags = args.detach ? ProjectService.RunFlags.Detach : ProjectService.RunFlags.None;

			switch (serviceDef.Protocol)
			{
				case BeamoProtocolType.HttpMicroservice:
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
					break;
				case BeamoProtocolType.EmbeddedMongoDb:
					runTasks.Add(args.BeamoLocalSystem.RunLocalEmbeddedMongoDb(serviceDef,
						beamoLocalManifest.EmbeddedMongoDbLocalProtocols[serviceDef.BeamoId]));
					break;
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
	}
	
	public static async Task RunService(
		RunProjectCommandArgs args,
		string serviceName,
		CancellationTokenSource serviceToken,
		ProjectService.BuildFlags buildFlags,
		ProjectService.RunFlags runFlags,
		Action<ProjectRunLogData> onLog = null,
		Action<ProjectErrorReport, int> onFailure = null,
		Action<float, string> onProgress=null)
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
			var commandStr = $"run --project {projectPath.EnquotePath()} --verbosity minimal -p:ErrorLog=\"{errorPath}%2Cversion=2\" -p:WarningLevel=0";

			if (buildFlags.HasFlag(ProjectService.BuildFlags.DisableClientCodeGen))
			{
				commandStr += " -p:GenerateClientCode=false";
			}
			
			if (runFlags.HasFlag(ProjectService.RunFlags.Detach))
			{
				// it varies based on the os, but in general, when we are detaching, then
				//  when THIS process exits, we don't want the child-process to exit. 
				//  The C# ProcessSDK makes that sort of difficult, but we can invoke programs
				//  that themselves create separate process trees. Or, at least I think we can.
				
				// in windows, this doesn't actually really _work_. It is put onto a background process, 
				//  and the main window may close for the parent, but the process is kept open
				//  if you look in task-manager. 
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					commandStr = "/C " + $"{exe.EnquotePath()} {commandStr}".EnquotePath('(', ')');
					exe = "cmd.exe";
				}
				else
				{
					commandStr = $"sh -c \"{exe} {commandStr}\" &";
					exe = "nohup";	
				}
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
				RedirectStandardOutput = true,
			};
			if (args.requireProcessId > 0)
			{
				startInfo.Environment.Add(Beamable.Common.Constants.EnvironmentVariables.BEAM_REQUIRE_PROCESS_ID, args.requireProcessId.ToString());
			}

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
					rawLogMessage = line, jsonData = null, forcedLogLevel = LogLevel.Error
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
							rawLogMessage = line, jsonData = null, forcedLogLevel = LogLevel.Error
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

			var shouldAutoKill = false;
			
			if (runFlags.HasFlag(ProjectService.RunFlags.Detach))
			{
				// wait for the progress to hit 1.
				while (!proc.HasExited && Math.Abs(currentProgress - 1) > .001f && !args.Lifecycle.IsCancelled)
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
					await cts.Task.WaitAsync(args.Lifecycle.CancellationToken);
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
					Log.Verbose("Killing sub process microservice.");
					proc.Kill(true);
				}
				catch (Exception ex)
				{
					// does not matter.
					Log.Verbose($"failed to kill microservice type=[{ex.GetType().Name}] message=[{ex.Message}]");
				}
			} else if (proc.HasExited && proc.ExitCode != 0)
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


