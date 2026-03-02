using System.CommandLine;
using cli.Services;
using cli.Utils;
using Markdig.Extensions.TaskLists;
using Spectre.Console;

namespace cli.Commands.Project;


public class ListReplacementTypeCommandArgs : CommandArgs
{
	public string UnrealProjectName;
}

public class ListReplacementTypeCommandOutput
{
	public List<ReplacementTypeInfo> ReplacementsTypes;
}
public class ListReplacementTypeCommand : AtomicCommand<ListReplacementTypeCommandArgs, ListReplacementTypeCommandOutput>
{
	public ListReplacementTypeCommand() : base("list-replacement-type", "List all replacement types for linked Unreal project")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--project-name", "The Unreal project name"), (args, i) => args.UnrealProjectName = i);
	}

	public override async Task<ListReplacementTypeCommandOutput> GetResult(ListReplacementTypeCommandArgs args)
	{
		var linkedEngineProjects = args.ConfigService.GetLinkedEngineProjects();
		var unrealProjects = linkedEngineProjects.unrealProjectsPaths;
		
		if (unrealProjects.Count == 0)
		{
			throw new CliException("No Unreal project linked, please link a project first with `beam project add-unreal-project <path>`");
		}
		if (unrealProjects.Count == 1 || args.Quiet)
		{
			var onlyItem = unrealProjects.First();
			return new ListReplacementTypeCommandOutput { ReplacementsTypes = onlyItem.ReplacementTypeInfos.ToList() };
		}

		var projectName = GetProjectName(args, unrealProjects);
		var selectedProject = unrealProjects.First(item => item.GetProjectName() == projectName);
		return new ListReplacementTypeCommandOutput { ReplacementsTypes = selectedProject.ReplacementTypeInfos.ToList() };
	}

	private static string GetProjectName(ListReplacementTypeCommandArgs args, HashSet<EngineProjectData.Unreal> unrealProjects)
	{
		if (!string.IsNullOrEmpty(args.UnrealProjectName))
		{
			return args.UnrealProjectName;
		}

		string projectSelection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which [green]project[/] do you want to add the Replacement Type?")
				.AddChoices(unrealProjects.Select(item => item.GetProjectName()))
				.AddBeamHightlight()
		);
		return projectSelection;
	}
}
