using cli.Services;

namespace cli.Version;

public class VersionCommandArgs : CommandArgs
{
}

public class VersionResults
{
	public string version, location, type, templates;
}


public class VersionCommand : AtomicCommand<VersionCommandArgs, VersionResults>, IStandaloneCommand, ISkipManifest, IIgnoreLogin
{
	public VersionCommand() : base("version", "Commands for managing the CLI version")
	{

	}

	public override void Configure()
	{

	}

	public override async Task<VersionResults> GetResult(VersionCommandArgs args)
	{
		var info = await args.DependencyProvider.GetService<VersionService>().GetInformationData(args.ProjectService);
		return new VersionResults
		{
			location = info.location,
			templates = info.templateVersion,
			type = info.installType.ToString(),
			version = info.version
		};
	}


}
