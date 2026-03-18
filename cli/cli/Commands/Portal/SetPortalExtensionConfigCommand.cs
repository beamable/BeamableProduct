using System.CommandLine;

namespace cli.Portal;

public class SetPortalExtensionConfigCommandArgs : CommandArgs
{
	public List<string> fileExtensionsToObserve;
}

public class SetPortalExtensionConfigCommandResults
{

}

public class SetPortalExtensionConfigCommand : AtomicCommand<SetPortalExtensionConfigCommandArgs, SetPortalExtensionConfigCommandResults>, ISkipManifest
{
	public SetPortalExtensionConfigCommand() : base("set-config", "Sets configuration for Portal Extensions")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<List<string>>("--file-extensions-to-observe",
			"Extra file extensions to include in the watch process (e.g., '.md,.json'). These run alongside default extensions") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore }, (arg, i) => arg.fileExtensionsToObserve = i);
	}

	public override Task<SetPortalExtensionConfigCommandResults> GetResult(SetPortalExtensionConfigCommandArgs args)
	{
		var portalExtensionConfig = new PortalExtensionConfig()
		{
			fileExtensionsToObserve = args.fileExtensionsToObserve,
		};

		args.ConfigService.SavePortalExtensionConfig(portalExtensionConfig);

		return Task.FromResult(new SetPortalExtensionConfigCommandResults());
	}
}
