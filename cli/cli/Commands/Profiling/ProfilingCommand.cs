namespace cli;

public class ProfilingCommandArgs : CommandArgs
{

}
public class ProfilingCommand : AppCommand<ProfilingCommandArgs>, IStandaloneCommand
{
	public ProfilingCommand() : base("profile", "Commands for inspecting profiling reports")
	{
	}

	public override void Configure()
	{

	}

	public override Task Handle(ProfilingCommandArgs args)
	{
		return Task.CompletedTask;
	}
}
