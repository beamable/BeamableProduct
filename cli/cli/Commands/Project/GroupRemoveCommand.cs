using cli.Services;

namespace cli.Commands.Project;

public class GroupRemoveCommand : AppCommand<UpdateGroupArgs>
{
	public GroupRemoveCommand() : base("rm", "Remove a group from a project")
	{
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument("Name of the service"), (args, i) => args.Name = i);
		AddArgument(GroupCommand.GroupsArgument, (args, i) => args.ToRemoveGroups = i);
	}
	
	public override Task Handle(UpdateGroupArgs args)
	{
		BeamoServiceDefinition definition = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Find(
			definition => definition.BeamoId == args.Name);
		if (definition == null)
		{
			throw new CliException($"Project not found, existing services: {string.Join(',', args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Select(serviceDefinition=>serviceDefinition.BeamoId))}'");
		}

		if (!args.IsValid())
		{
			throw new CliException("Invalid group name, should be just Ascii letters");
		}

		if (!definition.ServiceGroupTags.Union(args.ToRemoveGroups).Any())
		{
			throw new CliException($"There is no groups {string.Join(',',args.ToRemoveGroups)} in project {args.Name}. Valid groups: {string.Join(',', definition.ServiceGroupTags)}");
		}
		args.BeamoLocalSystem.SetBeamGroups(args);
		return Task.CompletedTask;
	}
}
