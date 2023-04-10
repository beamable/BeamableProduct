using Beamable.Common.BeamCli;
using NetMQ;
using Serilog;
using System.CommandLine;

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
	
}

public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, SampleNumber>
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
			this.SendResults(new SampleNumber{ x = i } );
			await Task.Delay(Random.Shared.Next(100));
		}
	}

}
