using Beamable.Common;
using Beamable.Common.BeamCli;
using cli.Services;
using cli.Utils;
using Errata;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using System.Text;

namespace cli;

public class ServicesRunCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToDeploy;
	public bool forceAmdCpuArchitecture = false;
}

public class ServicesRunCommand : AppCommand<ServicesRunCommandArgs>,
	IResultSteam<DefaultStreamResultChannel, ServiceRunReportResult>,
	IResultSteam<ServiceRunProgressResult, ServiceRunProgressResult>

{
	private BeamoLocalSystem _localBeamo;

	public ServicesRunCommand() :
		base("run",
			"Run services locally in Docker. Will fail if no docker instance is running in the local machine")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to deploy. Ignoring this option deploys all services") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToDeploy = i.Length == 0 ? null : i);
		AddOption(
			new Option<bool>(new string[] { "--force-amd-cpu-arch", "-fcpu" }, () => false,
				"Force the services to run with amd64 CPU architecture, useful when deploying from computers with ARM architecture"),
			(args, i) => args.forceAmdCpuArchitecture = i);
	}

	public override async Task Handle(ServicesRunCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;

		var isDockerRunning = await _localBeamo.CheckIsRunning();
		if (!isDockerRunning)
		{
			throw CliExceptions.DOCKER_NOT_RUNNING;
		}

		try
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest,
				_localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
			await _localBeamo.StartListeningToDocker();
		}
		catch
		{
			return;
		}

		// If no ids were given, run all registered services in docker 
		args.BeamoIdsToDeploy ??= _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();

		var failedId = args.BeamoIdsToDeploy.Where(id => !_localBeamo.VerifyCanBeBuiltLocally(id)).ToList();
		if (failedId.Any())
		{
			var diagnostics = new List<Diagnostic>();
			foreach (string id in failedId)
			{
				if (_localBeamo.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(id, out var microservice))
				{
					var path = args.ConfigService.GetRelativePath(microservice.DockerBuildContextPath);
					if (!Directory.Exists(path))
						diagnostics.Add(new Diagnostic($"[{id}] DockerBuildContext doesn't exist: [{path}]"));
					var dockerfilePath = Path.Combine(path, microservice.RelativeDockerfilePath);
					if (!File.Exists(dockerfilePath))
						diagnostics.Add(new Diagnostic($"[{id}] No Dockerfile found at path: [{dockerfilePath}]"));
				}
				else if (_localBeamo.BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(id, out var storage))
				{
					if ((bool)!storage.BaseImage?.Contains("mongo:"))
						diagnostics.Add(new Diagnostic($"[{id}] Base Image [{storage.BaseImage}] must be a version of mongo."));
						
				}
			}
			throw new CliException($"Some of the services could not be run locally: {string.Join(',',failedId)}", 555,true,null,diagnostics);
		}

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var allProgressTasks = args.BeamoIdsToDeploy.Where(id => _localBeamo.VerifyCanBeBuiltLocally(id)).Select(id => ctx.AddTask($"Deploying Service {id}")).ToList();
				try
				{
					await _localBeamo.DeployToLocal(_localBeamo.BeamoManifest, args.BeamoIdsToDeploy,
						forceAmdCpuArchitecture: args.forceAmdCpuArchitecture,
						(beamoId, progress) =>
						{
							var progressTask = allProgressTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							progressTask?.Increment((progress * 80) - progressTask.Value);
							this.SendResults<ServiceRunProgressResult, ServiceRunProgressResult>(new ServiceRunProgressResult() { BeamoId = beamoId, LocalDeployProgress = progressTask?.Value ?? 0.0f, });
						}, beamoId =>
						{
							var progressTask = allProgressTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							progressTask?.Increment(20);
							this.SendResults<ServiceRunProgressResult, ServiceRunProgressResult>(new ServiceRunProgressResult() { BeamoId = beamoId, LocalDeployProgress = progressTask?.Value ?? 0.0f, });
						});
				}
				catch (CliException e)
				{
					if (e.Message.Contains("cyclical", StringComparison.InvariantCultureIgnoreCase))
						AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
					else
					{
						this.SendResults<DefaultStreamResultChannel, ServiceRunReportResult>(new ServiceRunReportResult() { Success = false, FailureReason = e.ToString() });
						throw;
					}
				}
			});

		this.SendResults<DefaultStreamResultChannel, ServiceRunReportResult>(new ServiceRunReportResult() { Success = true, FailureReason = "" });

		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();

		await _localBeamo.StopListeningToDocker();
	}
}

public class ServiceRunProgressResult : IResultChannel
{
	public string ChannelName => "local_progress";

	public string BeamoId;
	public double LocalDeployProgress;
}

public class ServiceRunReportResult
{
	public bool Success;

	public string FailureReason;
}
