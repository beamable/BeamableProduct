using Beamable.Server;
using System.CommandLine.Help;

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
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}
