using cli.Services;

namespace cli.Commands.Project;

public class GroupAddCommand : AppCommand<UpdateGroupArgs>
{
	public GroupAddCommand() : base("add", "Add a group to a project")
	{
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument("Name of the service"), (args, i) => args.Name = i);
		AddArgument(GroupCommand.GroupsArgument, (args, i) => args.ToAddGroups = i);
	}
	
	public override Task Handle(UpdateGroupArgs args)
	{
		
		BeamoServiceDefinition definition = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Find(
			definition => definition.BeamoId == args.Name);
		if (definition == null)
		{
			throw new CliException("Project not found");
		}

		if (!args.IsValid())
		{
			throw new CliException("Invalid group name, should be just Ascii letters");
		}
		args.BeamoLocalSystem.SetBeamGroups(args);
		return Task.CompletedTask;
	}
}
