using Newtonsoft.Json;
using Spectre.Console;

namespace cli;
public class BeamoCommandArgs : CommandArgs { }

public class BeamoCommand : AppCommand<BeamoCommandArgs>
{
	private readonly BeamoService _beamoService;

	public BeamoCommand(BeamoService beamoService) : base("beamo", "outputs status call to console")
	{
		_beamoService = beamoService;
	}
	public override void Configure()
	{
		// nothing to do.
	}

	public override async Task Handle(BeamoCommandArgs args)
	{
		var response = await AnsiConsole.Status()
		                                .Spinner(Spinner.Known.Default)
		                                .StartAsync("Sending Request...", async ctx =>

			                                            await _beamoService.GetStatus()
		                                );
		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}

