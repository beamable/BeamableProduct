using cli.Services;

namespace cli.Commands.Project;

public class GroupRemoveCommand : AppCommand<GroupModifyCommandArgs>
{
	public GroupRemoveCommand() : base("rm", "Remove a group from a project")
	{
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument("Name of the service"), (args, i) => args.ProjectName = i);
		AddArgument(new ServiceNameArgument("Name of the group"), (args, i) => args.GroupName = i);
	}
	
	public override Task Handle(GroupModifyCommandArgs args)
	{
		BeamoServiceDefinition definition = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Find(
			definition => definition.BeamoId == args.ProjectName);
		if (definition == null)
		{
			throw new CliException($"Project not found, existing services: {string.Join(',', args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Select(definition=>definition.BeamoId))}'");
		}

		if (!args.GroupName.All(char.IsAsciiLetter) || string.IsNullOrEmpty(args.GroupName))
		{
			throw new CliException("Invalid group name, should be just Ascii letters");
		}

		if (!definition.ServiceGroupTags.Contains(args.GroupName))
		{
			throw new CliException($"There is no group {args.GroupName} in project {args.ProjectName}. Valid groups: {string.Join(',', definition.ServiceGroupTags)}");
		}
		var newGroupValue = definition.ServiceGroupTags.Where(tag => tag != args.GroupName).ToList();
		
		definition.SetBeamGroup(newGroupValue, args.ConfigService);
		return Task.CompletedTask;
	}
}
