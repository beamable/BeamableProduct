using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class BeamoManifestsCommand : AppCommand<BeamoManifestsArgs>
{
	private readonly BeamoService _beamoService;
	
	public BeamoManifestsCommand(BeamoService beamoService) : base("manifests", "outputs manifests json to console")
	{
		_beamoService = beamoService;
	}
	public override void Configure()
	{
		AddOption(new LimitOption(), (args, i) => args.limit = i);
		AddOption(new SkipOption(), (args, i) => args.skip = i);
	}

	public override async Task Handle(BeamoManifestsArgs args)
	{
		var response = await AnsiConsole.Status()
		                                .Spinner(Spinner.Known.Default)
		                                .StartAsync("Sending Request...", async ctx =>

			                                            await _beamoService.GetManifests()
		                                );
		response = response.Skip(args.skip).Take(args.limit > 0 ? args.limit : int.MaxValue).ToList();
		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}

public class BeamoManifestsArgs : CommandArgs
{
	public int limit = 0;
	public int skip = 0;
}
