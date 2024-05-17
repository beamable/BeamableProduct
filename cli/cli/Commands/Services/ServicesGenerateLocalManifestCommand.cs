using cli.Services;
using CliWrap;

namespace cli;

public class ServicesGenerateLocalManifestCommandArgs : CommandArgs
{
	
}

public class ServicesGenerateLocalManifestCommandOutput
{
	
}
public class ServicesGenerateLocalManifestCommand : AtomicCommand<ServicesGenerateLocalManifestCommandArgs, ServicesGenerateLocalManifestCommandOutput>, IStandaloneCommand
{
	public ServicesGenerateLocalManifestCommand() : base("generate-manifest", "Generate a local manifest by scraping local files")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task<ServicesGenerateLocalManifestCommandOutput> GetResult(ServicesGenerateLocalManifestCommandArgs args)
	{
		var rootFolder = args.ConfigService.BaseDirectory;



		var results = ProjectContextUtil.FindCsharpProjects(args.AppContext.DotnetPath, rootFolder).ToBlockingEnumerable().ToArray();
		
		
		return Task.FromResult(new ServicesGenerateLocalManifestCommandOutput());
	}
}
