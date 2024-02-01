using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using CliWrap;
using CliWrap.Buffered;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Text;

namespace cli.Commands.Project;


public class RunProjectCommandArgs : CommandArgs
{
	public ServiceName serviceId;
}

public class RunProjectCommandOutput
{
	
}


public class RunProjectCommand : StreamCommand<RunProjectCommandArgs, RunProjectCommandOutput>
{
	public RunProjectCommand() 
		: base("run", "Run a project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>(
			name: "service id", description: "The id of the project to run", getDefaultValue: () => default),
			(args, i) => args.serviceId = i);
	}

	public override async Task Handle(RunProjectCommandArgs args)
	{
		
		var localServices = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols;

		HttpMicroserviceLocalProtocol service = null;
		string serviceName = args.serviceId;
		if (string.IsNullOrEmpty(args.serviceId) && localServices.Count == 1)
		{
			var onlyServiceKvp = localServices.FirstOrDefault();
			service = onlyServiceKvp.Value;
			serviceName = onlyServiceKvp.Key;
			Log.Warning($"No service id given, but because only 1 service exists, proceeding automatically with id=[{serviceName}]");
		} else if (!localServices.TryGetValue(args.serviceId, out service))
		{
			throw new CliException(
				$"The given id=[{args.serviceId}] does not match any local services in the local beamo manifest.");
		}
		
		
		Log.Debug($"Found service definition, ctx=[{service.DockerBuildContextPath}] dockerfile=[{service.RelativeDockerfilePath}]");
		var dockerfilePath = Path.Combine(args.ConfigService.GetRelativePath(service.DockerBuildContextPath), service.RelativeDockerfilePath);
		var projectPath = Path.GetDirectoryName(dockerfilePath);
		var logPath = Path.Combine(args.ConfigService.ConfigFilePath, "temp", "logs", $"{serviceName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-logs.txt");
		Log.Debug($"service path=[{projectPath}]");
		
		var commandStr = $"watch --non-interactive --project {projectPath} run --property:CopyToLinkedProjects=false;GenerateClientCode=false --";
		using var cts = new CancellationTokenSource();
		var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithEnvironmentVariables(new Dictionary<string, string>
			{
				["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1",
				["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
				["LOG_PATH"] = logPath,
				["WATCH_TOKEN"] = "true"
			})
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				// TODO: capture build errors

				/*
				 * [07:25:54:5669 INF] dotnet watch : Started
				   [07:25:55:3243 INF] 
				   [07:25:55:5786 INF] dotnet watch : File changed: ./tuna.cs.
				   [07:25:55:7005 INF] dotnet watch : /Users/chrishanna/Documents/SAM/tuna/services/tuna/tuna.cs(13,16): error CS1525: Invalid expression term ';'
				   
				 */
				// Log.Information(line);
				if (line.Trim() == "dotnet watch : Exited")
				{
					cts.Cancel();
				}
				
				Console.Error.WriteLine("(watch) " + line);
			}))
			.ExecuteAsync(cts.Token);


		var tailArgs = args.Create<TailLogsCommandArgs>();
		tailArgs.reconnect = true;
		tailArgs.service = new ServiceName(serviceName);

		var logCommand = ProjectLogsService.Handle(tailArgs, logMessage =>
		{
			var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
			Console.Error.WriteLine($"[{parsed.logLevel}] {parsed.message}");
			parsed.raw = logMessage;
		}, cts.Token);

		try
		{
			await command;
		}
		catch (OperationCanceledException)
		{
			Log.Debug("watch command was cancelled.");
		}

		await logCommand;

	}
}
