using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Help;

namespace cli.TempCommands;

public class TempCommandGroup : CommandGroup
{
	public TempCommandGroup() : base("temp", "Commands for the .beamable temp folder")
	{
	}
}

public class TempClearArgs : CommandArgs
{

}
public class TempClearCommandGroup : CommandGroup<TempClearArgs>
{
	public TempClearCommandGroup() : base("clear", "Commands to clear files in the temp folder")
	{
	}

	public override void Configure()
	{

	}

	public override Task Handle(TempClearArgs args)
	{
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}
