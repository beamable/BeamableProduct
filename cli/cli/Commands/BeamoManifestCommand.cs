using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class BeamoManifestCommand : AppCommand<BeamoManifestArgs>
{
	private readonly BeamoService _beamoService;
	
	public BeamoManifestCommand(BeamoService beamoService) : base("manifest", "outputs manifest json to console")
	{
		_beamoService = beamoService;
	}
	public override void Configure()
	{
	}

	public override async Task Handle(BeamoManifestArgs args)
	{
		var response = await AnsiConsole.Status()
		                                .Spinner(Spinner.Known.Default)
		                                .StartAsync("Sending Request...", async ctx =>

			                                            await _beamoService.GetCurrentManifest()
		                                );
		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}

public class BeamoManifestArgs : CommandArgs { }
