using Beamable.Common.Semantics;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class GroupCommand : CommandGroup
{
	public static Argument<List<string>> GroupsArgument = new("groups", "Beamable groups for this service")
	{
		Arity = ArgumentArity.ZeroOrMore,

	};
	public GroupCommand() : base("group", "List Service Groups")
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


public class UpdateGroupArgs : CommandArgs
{
	public ServiceName Name { get; set; }
	public List<string> ToAddGroups { get; set; } = new();
	public List<string> ToRemoveGroups { get; set; } = new();
	public bool IsValid()
	{
		var groupNames = ToRemoveGroups.Union(ToAddGroups).ToArray();
		return groupNames.All(s => s.All(char.IsAsciiLetter)) && !groupNames.Any(string.IsNullOrWhiteSpace);
	}
}
