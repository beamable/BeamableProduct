using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli.Commands.Project;

public class BuildProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool watch;
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
	}

	public override async Task Handle(BuildProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);
		var serviceTasks = new List<Task>();
		foreach (var service in args.services)
		{
			serviceTasks.Add(ProjectService.WatchBuild(args, new ServiceName(service), (report) =>
			{
				SendResults(new BuildProjectCommandOutput
				{
					service = service,
					report = report
				});
			}));
		}

		await Task.WhenAll(serviceTasks);
	}
}
