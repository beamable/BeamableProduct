using Beamable.Common.Semantics;
using Beamable.Server;

namespace cli.Commands.Project;

public class ProjectDependencies : AppCommand<ProjectDependenciesArgs>
{
	public ProjectDependencies() : base("dependencies", "List project dependencies")
	{
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument("Name of the service"), (args, i) => args.ProjectName = i);
	}

	public override Task Handle(ProjectDependenciesArgs args)
	{
		return Task.Run(() =>
		{
			var deps = args.BeamoLocalSystem.GetDependencies(args.ProjectName);
			Log.Information("{0} dependencies: {1}", args.ProjectName, string.Join(',', deps));
		});
	}
}

public class ProjectDependenciesArgs : CommandArgs
{
	public ServiceName ProjectName;
}
