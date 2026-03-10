using Beamable.Server;
using CliWrap;
using System.CommandLine.Help;

namespace cli.Portal;

public class PortalExtensionCommandArgs : CommandArgs
{

}

public class PortalExtensionCommand : AppCommand<PortalExtensionCommandArgs>
{
	public PortalExtensionCommand() : base("extension", "Gives access to all Portal Extensions related commands")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PortalExtensionCommandArgs args)
	{
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}
