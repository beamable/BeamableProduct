using Beamable.Common.BeamCli;
using NetMQ;
using Serilog;

namespace cli.Dotnet;

public class CheckStatusCommandArgs : CommandArgs
{
	
}
public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
{
	public CheckStatusCommand() : base("ps", "List the running status of local services not running in docker")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(CheckStatusCommandArgs args)
	{
		for (var i = 0; i < 10; i++)
		{
			Log.Information("Printing " + i);
			args.Reporter.Report("nums", new SampleNumber(){x = i});
			Log.Information("did the print");
			await Task.Delay(Random.Shared.Next(100));
		}
	}
}
