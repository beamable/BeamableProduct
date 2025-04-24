using Beamable.Common;
using cli.Services;

namespace cli.OtelCommands;


[Serializable]
public class StartCollectorCommandArgs : CommandArgs
{
	public bool watch;
}

public class StartCollectorCommand : AppCommand<StartCollectorCommandArgs>
{
	public StartCollectorCommand() : base("start", "Starts the collector process")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(StartCollectorCommandArgs args)
	{
		await CollectorManager.StartCollectorProcess(true, args.Lifecycle.Source);
	}
}
