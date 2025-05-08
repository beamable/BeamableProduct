using cli.Services;
using CliWrap;

namespace cli;

public class ServicesGenerateLocalManifestCommandArgs : CommandArgs
{
	
}

[Serializable]
public class ServicesGenerateLocalManifestCommandOutput
{
	public BeamoLocalManifest manifest;
}
public class ServicesGenerateLocalManifestCommand : AtomicCommand<ServicesGenerateLocalManifestCommandArgs, ServicesGenerateLocalManifestCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public ServicesGenerateLocalManifestCommand() : base("generate-manifest", "Generate a local manifest by scraping local files")
	{
	}

	public override void Configure()
	{
		
	}

	public override async Task<ServicesGenerateLocalManifestCommandOutput> GetResult(ServicesGenerateLocalManifestCommandArgs args)
	{
		var manifest = await ProjectContextUtil.GenerateLocalManifest( args.AppContext.DotnetPath,  args.BeamoService, args.ConfigService, args.AppContext.IgnoreBeamoIds);
		return new ServicesGenerateLocalManifestCommandOutput
		{
			manifest = manifest
		};
	}
}
