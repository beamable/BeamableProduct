using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class BeamoCurrentManifestCommand : AppCommand<BeamoManifestArgs>
{
	private readonly BeamoService _beamoService;
	
	public BeamoCurrentManifestCommand(BeamoService beamoService) : base("current", "outputs current manifest json to console")
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

public class BeamoManifestArgs : CommandArgs
{
	public string jsonPath;
}
