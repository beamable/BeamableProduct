using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;

namespace cli.Commands.Project.Deps;

public class DepsCommandArgs : CommandArgs
{
	
}

public class DepsCommand : AppCommand<DepsCommandArgs>, IEmptyResult
{
	public DepsCommand() : base("deps", "Allow access to dependencies related commands")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(DepsCommandArgs args)
	{
		
		return Task.CompletedTask;
	}
}
