﻿using Beamable.Common;
using Beamable.Common.BeamCli;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesRunCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToDeploy;
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
	}

	public override async Task Handle(ServicesRunCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;

		var isDockerRunning = await _localBeamo.CheckIsRunning();
		if (!isDockerRunning)
		{
			throw new CliException("Docker is not running in this machine. Please start Docker before running this command.", Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_DOCKER_NOT_RUNNING, true);
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

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var allProgressTasks = args.BeamoIdsToDeploy.Where(id => _localBeamo.VerifyCanBeBuiltLocally(id)).Select(id => ctx.AddTask($"Deploying Service {id}")).ToList();
				try
				{
					await _localBeamo.DeployToLocal(_localBeamo.BeamoManifest, args.BeamoIdsToDeploy,
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
