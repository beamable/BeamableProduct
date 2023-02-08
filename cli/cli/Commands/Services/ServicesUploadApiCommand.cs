using cli.Services;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesUploadApiCommandArgs : LoginCommandArgs
{
}

public class ServicesUploadApiCommand : AppCommand<ServicesUploadApiCommandArgs>
{
	private BeamoService _remoteBeamo;

	public ServicesUploadApiCommand() :
		base("upload-api",
			"Gets the URL that we upload docker images into when deploying services remotely for this realm.")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(ServicesUploadApiCommandArgs args)
	{
		_remoteBeamo = args.BeamoService;

		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetUploadApi()
			);

		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}
