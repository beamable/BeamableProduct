using Beamable.Common.Semantics;
using Spectre.Console;

namespace cli.Commands.Project;

public class GroupCommand : CommandGroup
{
	public GroupCommand() : base("group", "Generate an ignore file in .beamable folder for given VCS")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(CommandGroupArgs args)
	{
		var table = new Table();
		table.AddColumn("Group name");
		table.AddColumn("Services IDs");
		foreach (var record in args.BeamoLocalSystem.BeamoManifest.ServiceGroupToBeamoIds)
		{
			table.AddRow(record.Key, string.Join(", ", record.Value));
		}
		if (args.BeamoLocalSystem.BeamoManifest.ServiceGroupToBeamoIds.Count > 0)
		{
			AnsiConsole.Write(table);
		}
		else
		{
			AnsiConsole.WriteLine("No services groups found");
		}

		return Task.CompletedTask;
	}
}


public class GroupModifyCommandArgs : CommandArgs
{
	public ServiceName ProjectName;
	public string GroupName;
}
