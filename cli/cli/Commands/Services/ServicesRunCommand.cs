using Beamable.Common;
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
			ServicesDeletionNotice.REMOVED_PREFIX + "Run services locally in Docker. Will fail if no docker instance is running in the local machine")
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
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.RUN_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
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
