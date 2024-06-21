using Newtonsoft.Json;
using Spectre.Console;

namespace cli.Commands.Project;

public class ShowRemoteManifestCommandArgs : CommandArgs
{

}

public class ShowRemoteManifestCommand : AppCommand<ShowRemoteManifestCommandArgs>
{
	public override bool IsForInternalUse => true;
	public ShowRemoteManifestCommand() : base("remote-manifest", "Returns the remote manifest in json format")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(ShowRemoteManifestCommandArgs args)
	{
		var manifest = await args.BeamoService.GetCurrentManifest();
		AnsiConsole.WriteLine(JsonConvert.SerializeObject(manifest, Formatting.Indented));
	}
}
