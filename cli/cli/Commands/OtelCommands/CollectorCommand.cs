using Beamable.Server;
using System.CommandLine.Help;

namespace cli.OtelCommands;

public class CollectorCommandArgs : CommandArgs
{

}

public class CollectorCommand : AppCommand<CollectorCommandArgs>, IEmptyResult
{
	public CollectorCommand() : base("collector", "Allows access to Open Telemetry collector related commands")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(CollectorCommandArgs args)
	{
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}
