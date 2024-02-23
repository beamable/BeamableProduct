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

public class ServicesTemplatesCommandOutput
{
	public List<ServiceTemplate> templates;
}

public class ServicesTemplatesCommand : AtomicCommand<ServicesTemplatesCommandArgs, ServicesTemplatesCommandOutput>
{
	private BeamoService _remoteBeamo;


	public ServicesTemplatesCommand() :
		base("templates",
			"Gets all the template types available in this realm")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ServicesTemplatesCommandOutput> GetResult(ServicesTemplatesCommandArgs args)
	{
		_remoteBeamo = args.BeamoService;
		List<ServiceTemplate> response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetTemplates()
			);

		var result = new ServicesTemplatesCommandOutput { templates = response };
		return result;
	}
}
