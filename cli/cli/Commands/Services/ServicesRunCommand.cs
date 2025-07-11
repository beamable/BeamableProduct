﻿using Beamable.Common;
using Beamable.Common.BeamCli;
using cli.Services;
using cli.Utils;
using Errata;
using Spectre.Console;
using System.CommandLine;
using Beamable.Server;

namespace cli;

public class ServicesRunCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToDeploy;
	public bool forceLocalCpu = false;
	public bool autoDeleteContainers;
	public bool forceAmdCpuArchitecture;
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
		//This option is deprecated, we only keep it in here to give better error message in case of anyone using it still
		AddOption(
			new Option<bool>(new string[] { "--force-amd-cpu-arch", "-fcpu" }, () => false,
				"[DEPRECATED] Force the services to run with amd64 CPU architecture, useful when deploying from computers with ARM architecture"),
			(args, i) => args.forceAmdCpuArchitecture = i);

		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to deploy. Ignoring this option deploys all services") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToDeploy = i.Length == 0 ? null : i);
		AddOption(
			new Option<bool>(new string[] { "--force-local-cpu", "-flcpu" }, () => false,
				"By default, this command forces the services to run with amd64 CPU architecture, which is the architecture used in Docker"),
			(args, i) => args.forceLocalCpu = i);


		
		AddOption(
			new Option<bool>(new string[] { "--keep-containers", "-k" }, () => false,
				"Automatically remove service containers after they exit"),
			
			// it is mildly confusing to invert the logic here, but I think there is a good reason.
			//  the default in docker is to require a user to specify --rm to remove the container, 
			//  as beamable, we should flip that auto clean for folks. 
			//  in that regard, the --keep-containers option needs to be set to NOT include the --rm
			(args, i) => args.autoDeleteContainers = !i);
		
	}

	public override async Task Handle(ServicesRunCommandArgs args)
	{
		if (args.forceAmdCpuArchitecture)
		{
			throw new CliException("Error: The option [--force-amd-cpu-arch, -fcpu] is obsolete. Amd CPU architecture is already being enforced by default");
		}

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
		catch (Exception ex)
		{
			Log.Error($"Failed to communicate with docker. message=[{ex.Message}]");
			throw;
			//throw new CliException($"Failed to communicate with docker. message=[{ex.Message}]");
		}

		// If no ids were given, run all registered services in docker 
		args.BeamoIdsToDeploy ??= _localBeamo.BeamoManifest.LocalBeamoIds;

		var failedId = args.BeamoIdsToDeploy.Where(id => !_localBeamo.VerifyCanBeBuiltLocally(id)).ToList();
		if (failedId.Any())
		{
			var diagnostics = new List<Diagnostic>();
			foreach (string id in failedId)
			{
				if (_localBeamo.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(id, out var microservice))
				{
					var path = args.ConfigService.GetRelativeToBeamableFolderPath(microservice.DockerBuildContextPath);
					if (!Directory.Exists(path))
						diagnostics.Add(new Diagnostic($"[{id}] DockerBuildContext doesn't exist: [{path}]"));
					var dockerfilePath = microservice.AbsoluteDockerfilePath;
					if (!File.Exists(dockerfilePath))
						diagnostics.Add(new Diagnostic($"[{id}] No Dockerfile found at path: [{dockerfilePath}]"));
				}
				else if (_localBeamo.BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(id, out var storage))
				{
					if ((bool)!storage.BaseImage?.Contains("mongo:"))
						diagnostics.Add(new Diagnostic($"[{id}] Base Image [{storage.BaseImage}] must be a version of mongo."));
				}
			}

			throw new CliException($"Some of the services could not be run locally: {string.Join(',', failedId)}", 555, true, null, diagnostics);
		}

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var allProgressTasks = args.BeamoIdsToDeploy.Where(id => _localBeamo.VerifyCanBeBuiltLocally(id)).Select(id => ctx.AddTask($"Deploying Service {id}")).ToList();
				try
				{
					List<string> idsToDeploy = new List<string>();
					idsToDeploy.AddRange(args.BeamoIdsToDeploy);
					foreach (string id in args.BeamoIdsToDeploy)
					{
						var dependencies = _localBeamo.GetDependencies(id);
						idsToDeploy.AddRange(dependencies.Select(d => d.name));
					}

					var uniqueIds = idsToDeploy.Distinct().ToArray();
					List<BeamoServiceDefinition> definitions = uniqueIds.Select(id =>
						args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
							.FirstOrDefault(def => def.BeamoId == id)).ToList();

					List<Promise<Unit>> promises = new List<Promise<Unit>>();
					foreach (var definition in definitions)
					{
						promises.Add(args.BeamoLocalSystem.UpdateDockerFile(definition));
					}

					var sequence = Promise.Sequence(promises);
					await sequence;

					await _localBeamo.DeployToLocal(_localBeamo, uniqueIds, !args.forceLocalCpu,
						autoDeleteContainers: args.autoDeleteContainers, 
						buildPullImageProgress: (beamoId, progress) =>
					{
						var progressTask = allProgressTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						progressTask?.Increment((progress * 80) - progressTask.Value);
						this.SendResults<ServiceRunProgressResult, ServiceRunProgressResult>(new ServiceRunProgressResult() { BeamoId = beamoId, LocalDeployProgress = progressTask?.Value ?? 0.0f, });
					}, onServiceDeployCompleted: beamoId =>
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
