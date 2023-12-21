using Beamable.Common;
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
	public string[] BeamoIdsToStop;
}

public class ServicesStopCommand : AppCommand<ServicesStopCommandArgs>, IEmptyResult
{
	public ServicesStopCommand() :
		base("stop", "Stops all your locally running containers for the selected Beamo Services")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to stop") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToStop = i.Length == 0 ? Array.Empty<string>() : i);
	}

	public override async Task Handle(ServicesStopCommandArgs args)
	{
		// we use the BeamCommandAssistantBuilder to construct the underlying reset container command
		await new BeamCommandAssistantBuilder("services reset", args.AppContext)
			.AddArgument("container")
			.WithOption(args.BeamoIdsToStop.Length > 0, "--ids", args.BeamoIdsToStop)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.WorkingDirectory), "--dir",
				args.AppContext.WorkingDirectory)
			.RunAsync();
	}
}
