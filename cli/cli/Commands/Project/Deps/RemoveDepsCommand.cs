using Serilog;
using System.CommandLine;

namespace cli.Commands.Project.Deps;

public class RemoveDepsCommandArgs : CommandArgs
{
	public string ServiceName;
	public string Dependency;
}

public class RemoveDepsCommand : AppCommand<RemoveDepsCommandArgs>, IEmptyResult
{
	public RemoveDepsCommand() : base("remove", "Remove the dependency between the given microservice and storage")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("microservice", description: "The microservice name that the dependency will be removed from"),
			(args, i) => args.ServiceName = i);
		AddArgument(new Argument<string>("dependency", description: "The dependency that will be removed from that service"),
			(args, i) => args.Dependency = i);
	}

	public override async Task Handle(RemoveDepsCommandArgs args)
	{
		if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(args.Dependency, out var dependencyDefinition))
		{
			Log.Information($"The dependency {args.Dependency} does not have a definition in the manifest");
			return;
		}

		if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(args.ServiceName, out var serviceDefinition))
		{
			Log.Information($"The service {args.ServiceName} does not have a definition in the manifest");
			return;
		}
		
		List<string> dependencies = await args.BeamoLocalSystem.GetDependencies(args.ServiceName);

		if (!dependencies.Contains(args.Dependency))
		{
			Log.Information($"The service {args.ServiceName} does not have {args.Dependency} as a dependency");
			return;
		}
		
		Log.Information("Removing {ArgsStorageName} reference from {Dependency}. ", args.Dependency, args.ServiceName);
		await args.BeamoLocalSystem.RemoveProjectDependency(serviceDefinition, dependencyDefinition);
	}
}
