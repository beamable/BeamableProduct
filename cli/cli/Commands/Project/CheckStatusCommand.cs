using NetMQ;

namespace cli.Dotnet;

public class CheckStatusCommandArgs : CommandArgs
{
	
}
public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
{
	public CheckStatusCommand() : base("ps", "list the running status of local services not running in docker")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(CheckStatusCommandArgs args)
	{
		
		return Task.CompletedTask;
	}
}
