using Beamable.Common;
using cli.Dotnet;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class ServicesStopCommandArgs : CommandArgs
{
	public List<string> services;
}

public class ServicesStopCommand : AppCommand<ServicesStopCommandArgs>, IEmptyResult
{
	public ServicesStopCommand() :
		base("stop", "Stops all your locally running containers for the selected Beamo Services")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(ServicesStopCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		await args.BeamoLocalSystem.SynchronizeInstanceStatusWithDocker(args.BeamoLocalSystem.BeamoManifest, args.BeamoLocalSystem.BeamoRuntime.ExistingLocalServiceInstances);
		await args.BeamoLocalSystem.StartListeningToDocker();

		await ServicesResetContainerCommand.TurnOffContainers(args.BeamoLocalSystem, args.services.ToArray(), _ =>
		{
			// do nothing.
		});
	}
}
