using cli.Services;

namespace cli.Commands.Project;

public class GroupAddCommand : AppCommand<GroupModifyCommandArgs>
{
	public GroupAddCommand() : base("add", "Add a group to a project")
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
			throw new CliException("Project not found");
		}

		if (!args.GroupName.All(char.IsAsciiLetter) || string.IsNullOrEmpty(args.GroupName))
		{
			throw new CliException("Invalid group name, should be just Ascii letters");
		}
		if (definition.ServiceGroupTags.Contains(args.GroupName))
		{
			throw new CliException("Group name already added");
		}
		var newGroupValue = definition.ServiceGroupTags.Concat(new [] { args.GroupName });
		
		definition.SetBeamGroup(newGroupValue, args.ConfigService);
		return Task.CompletedTask;
	}
}
