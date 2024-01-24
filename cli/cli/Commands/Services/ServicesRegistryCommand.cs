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

public class ServicesRegistryOutput
{
	public string registryUrl;
}

public class ServicesRegistryCommand : AtomicCommand<ServicesRegistryCommandArgs, ServicesRegistryOutput>
{
	private BeamoService _remoteBeamo;


	public ServicesRegistryCommand() :
		base("registry",
			"Gets the docker registry URL that we upload docker images into when deploying services remotely for this realm")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ServicesRegistryOutput> GetResult(ServicesRegistryCommandArgs args)
	{
		_remoteBeamo = args.BeamoService;

		string response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetDockerImageRegistry()
			);

		Console.Error.WriteLine(response);
		return new ServicesRegistryOutput { registryUrl = response };
	}
}
