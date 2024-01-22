using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class ServicesManifestsCommand : AtomicCommand<ServicesManifestsArgs, ServiceManifestOutput>
{
	private BeamoService _beamoService;

	public ServicesManifestsCommand() : base("manifests", "Outputs manifests json to console")
	{
	}
	public override void Configure()
	{
		AddOption(new LimitOption(), (args, i) => args.limit = i);
		AddOption(new SkipOption(), (args, i) => args.skip = i);
	}

	public override async Task<ServiceManifestOutput> GetResult(ServicesManifestsArgs args)
	{
		_beamoService = args.BeamoService;

		List<ServiceManifest> response = await AnsiConsole.Status()
										.Spinner(Spinner.Known.Default)
										.StartAsync("Sending Request...", async ctx =>

														await _beamoService.GetManifests()
										);
		response = response.Skip(args.skip).Take(args.limit > 0 ? args.limit : int.MaxValue).ToList();

		var result = new ServiceManifestOutput { manifests = response };
		return PrintResult(result);
	}
}

public class ServicesManifestsArgs : CommandArgs
{
	public int limit = 0;
	public int skip = 0;
}

public class ServiceManifestOutput
{
	public List<ServiceManifest> manifests;
}
