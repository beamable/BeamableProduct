using cli.Services;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesTemplatesCommandArgs : LoginCommandArgs
{
}

public class ServicesTemplatesCommand : AppCommand<ServicesTemplatesCommandArgs>
{
	private BeamoService _remoteBeamo;


	public ServicesTemplatesCommand() :
		base("templates",
			"Gets all the template types available in this realm.")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(ServicesTemplatesCommandArgs args)
	{
		_remoteBeamo = args.BeamoService;
		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetTemplates()
			);

		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}
