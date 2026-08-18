using Beamable.Server;
using System.CommandLine.Help;

namespace cli.Web;

public class WebCommandArgs : CommandArgs
{
}

/// <summary>
/// Parent group for the local web-package dev loop: publishing locally-built <c>@beamable/sdk</c> and
/// <c>@beamable/portal-toolkit</c> to a local Verdaccio registry and wiring projects up to them.
/// Replaces the setup-web.sh / dev-web.sh / teardown-web.sh scripts.
/// </summary>
public class WebCommand : AppCommand<WebCommandArgs>, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse => true;

	public WebCommand() : base("web", "Gives access to all local Beamable web package (SDK and Portal Toolkit) commands")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(WebCommandArgs args)
	{
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}
