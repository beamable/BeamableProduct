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

public class ServicesUploadApiOutput
{
	public string uploadUrl;
}

public class ServicesUploadApiCommand : AtomicCommand<ServicesUploadApiCommandArgs, ServicesUploadApiOutput>
{
	private BeamoService _remoteBeamo;

	public ServicesUploadApiCommand() :
		base("upload-api",
			"Gets the URL that we upload docker images into when deploying services remotely for this realm")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ServicesUploadApiOutput> GetResult(ServicesUploadApiCommandArgs args)
	{
		_remoteBeamo = args.BeamoService;

		string response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetUploadApi()
			);

		Console.Error.WriteLine(response);
		return new ServicesUploadApiOutput { uploadUrl = response };
	}
}
