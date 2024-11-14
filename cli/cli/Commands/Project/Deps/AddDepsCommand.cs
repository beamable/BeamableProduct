using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli.Commands.Project.Deps;

public class AddDepsCommandArgs : CommandArgs
{
	public string ServiceName;
	public string Dependency;
}

public class AddDepsCommand : AppCommand<AddDepsCommandArgs>, IEmptyResult
{
	public AddDepsCommand() : base("add", "Add a given storage as a dependency of a microservice")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("microservice", description: "The microservice name that the dependency will be added to"),
			(args, i) => args.ServiceName = i);
		AddArgument(new Argument<string>("dependency", description: "The storage that will be a dependency of the given microservice"),
			(args, i) => args.Dependency = i);
	}

	public override async Task Handle(AddDepsCommandArgs args)
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

		List<DependencyData> dependencies = args.BeamoLocalSystem.GetDependencies(args.ServiceName);
		bool isAlreadyDependency = dependencies.Any(data => data.name == args.Dependency);

		if (isAlreadyDependency)
		{
			Log.Information($"The service {args.ServiceName} already has {args.Dependency} as a dependency");
			return;
		}

		Log.Information("Adding {ArgsStorageName} reference to {Dependency}. ", args.Dependency, args.ServiceName);
		await args.BeamoLocalSystem.AddProjectDependency(serviceDefinition, dependencyDefinition.AbsoluteProjectDirectory);
	}
}
