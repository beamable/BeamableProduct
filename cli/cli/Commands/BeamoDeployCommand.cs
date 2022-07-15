using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class BeamoDeployCommand : AppCommand<BeamoDeployArgs>
{
	private readonly BeamoService _beamoService;
	
	public BeamoDeployCommand(BeamoService beamoService) : base("deploy", "outputs current manifest json to console")
	{
		_beamoService = beamoService;
	}
	public override void Configure()
	{
		AddOption(new DeployFilePathOption(), (args, i) => args.jsonPath = i);
	}

	public override async Task Handle(BeamoDeployArgs args)
	{
		if (!File.Exists(args.jsonPath))
		{
			throw new FileNotFoundException($"File {args.jsonPath} not found");
		}

		var fileContent = await File.ReadAllTextAsync(args.jsonPath);
		var manifest = JsonConvert.DeserializeObject<ServiceManifest>(fileContent);
		if (manifest == null)
		{
			throw new JsonException("Failed to convert file content to ServiceManifest");
		}
		await AnsiConsole.Status()
		                                .Spinner(Spinner.Known.Default)
		                                .StartAsync("Sending Request...", async ctx =>

			                                            await _beamoService.Deploy(manifest)
		                                );
	}
}

public class BeamoDeployArgs : CommandArgs
{
	public string jsonPath;
}
