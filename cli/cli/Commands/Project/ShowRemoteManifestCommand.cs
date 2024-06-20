using cli.Services;

namespace cli.Commands.Project;

public class ShowRemoteManifestCommandArgs : CommandArgs
{

}

public class ShowRemoteManifestCommand : AtomicCommand<ShowRemoteManifestCommandArgs, ServiceManifest>
{
	public override bool IsForInternalUse => true;
	public ShowRemoteManifestCommand() : base("remote-manifest", "Returns the remote manifest in json format")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ServiceManifest> GetResult(ShowRemoteManifestCommandArgs args)
	{
		var manifest = await args.BeamoService.GetCurrentManifest();
		return manifest;
	}
}
