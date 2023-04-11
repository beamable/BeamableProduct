using Beamable.Common.BeamCli;
using NetMQ;
using Serilog;
using System.CommandLine;

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
	
}

public class CheckStatusStreamResult
{
	public string cid, pid;
}

public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, CheckStatusStreamResult>
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
			this.SendResults(new CheckStatusStreamResult
			{
				cid = args.AppContext.Cid,
				pid = args.AppContext.Pid
			} );
			await Task.Delay(Random.Shared.Next(100));
		}
	}

}
