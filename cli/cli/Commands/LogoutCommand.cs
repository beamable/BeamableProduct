namespace cli;

public class LogoutCommandArgs : CommandArgs
{
	
}

public class LogoutCommandResult
{
	
}
public class LogoutCommand 
	: AtomicCommand<LogoutCommandArgs, LogoutCommandResult>
	, ISkipManifest
{
	public LogoutCommand() : base("logout", "Removes any saved credentials")
	{
	}

	public override void Configure()
	{
	}

	public override Task<LogoutCommandResult> GetResult(LogoutCommandArgs args)
	{
		args.ConfigService.DeleteTokenFile();
		return Task.FromResult(new LogoutCommandResult());
	}
}
