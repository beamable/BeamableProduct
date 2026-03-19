using System.CommandLine;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;

namespace cli.Commands.Project;

public class BuildProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool watch;
	public string stopReason;
	public int MaxParallelTask;
}

public class BuildProjectCommandOutput
{
	public string service;
	public ProjectErrorReport report;
}

public class BuildProjectCommand : StreamCommand<BuildProjectCommandArgs, BuildProjectCommandOutput>
{
	public BuildProjectCommand() : base("build", "Build and monitor for compile errors")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.watch = i);
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		AddOption(new Option<string>("--stop-reason", "A message to send to the running service when it is terminated"),
			(args, i) => args.stopReason = i);
		AddOption(
			new Option<int>(new string[] { "--max-parallel-count", "-mpc" }, () => 0,
				"Maximum number of parallel services builds"), (args, i) => args.MaxParallelTask = i);
	}

	public override async Task Handle(BuildProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);
		var serviceTasks = new List<Task>();
		foreach (var service in args.services)
		{
			serviceTasks.Add(ProjectService.WatchBuild(args, new ServiceName(service), ProjectService.BuildFlags.None, (report) =>
			{
				SendResults(new BuildProjectCommandOutput
				{
					service = service,
					report = report
				});
			}, serviceStopReason: args.stopReason));
		}

		await Task.WhenAll(serviceTasks);
	}
}
