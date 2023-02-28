using System.CommandLine;

namespace cli;

public class CheckCountersCommandArgs : CommandArgs
{
	public string countersJsonFilePath;
}

public class CheckCountersCommand : AppCommand<CheckCountersCommandArgs>
{
	public CheckCountersCommand() : base("check-counters", "read the results of a dotnet-counters json file and determine if there are errors")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("counters-file-path", "the path to the dotnet-counters output json file"), (args, i) => args.countersJsonFilePath = i);
	}

	public override Task Handle(CheckCountersCommandArgs args)
	{
		return Task.CompletedTask;
	}
}

public class DotnetCounterEntry
{
	
}
