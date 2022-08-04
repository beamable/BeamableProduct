using cli.Services;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesRegistryCommandArgs : LoginCommandArgs
{
}

public class ServicesRegistryCommand : AppCommand<ServicesRegistryCommandArgs>
{
	private readonly BeamoService _remoteBeamo;


	public ServicesRegistryCommand(BeamoService remoteBeamo) :
		base("registry",
			"Gets the docker registry URL that we upload docker images into when deploying services remotely for this realm.")
	{
		_remoteBeamo = remoteBeamo;
	}

	public override void Configure()
	{
	}

	public override async Task Handle(ServicesRegistryCommandArgs args)
	{
		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetDockerImageRegistry()
			);

		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}
