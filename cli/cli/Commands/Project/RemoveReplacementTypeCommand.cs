using System.CommandLine;
using cli.Services;
using cli.Utils;
using Markdig.Extensions.TaskLists;
using Spectre.Console;

namespace cli.Commands.Project;


public class RemoveReplacementTypeCommandArgs : CommandArgs
{
	public string ReferenceId;
	public string UnrealProjectName;
}
public class RemoveReplacementTypeCommand : AppCommand<RemoveReplacementTypeCommandArgs>, IEmptyResult
{
	public RemoveReplacementTypeCommand() : base("remove-replacement-type", "Add a replacement type for all Unreal linked projects")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--reference-id", "The reference Id (C# class/struct name) for the replacement"), (args, i) => args.ReferenceId = i);
		AddOption(new Option<string>("--project-name", "The Unreal project name"), (args, i) => args.UnrealProjectName = i);
	}

	public override async Task Handle(RemoveReplacementTypeCommandArgs args)
	{
		
		var referenceId = await GetReferenceId(args);
		
		var linkedEngineProjects = args.ConfigService.GetLinkedEngineProjects();
		var unrealProjects = linkedEngineProjects.unrealProjectsPaths;
		
		if (unrealProjects.Count == 0)
		{
			throw new CliException("No Unreal project linked, please link a project first with `beam project add-unreal-project <path>`");
		}
		
		// Check if it has specific project name
		if (!string.IsNullOrEmpty(args.UnrealProjectName))
		{
			if (unrealProjects.Any(item => item.GetProjectName() == args.UnrealProjectName))
			{
				var projectData = unrealProjects.FirstOrDefault(item => item.GetProjectName() == args.UnrealProjectName);
				RemoveReferenceAndSetLinkedEngineProjects(args, linkedEngineProjects, projectData, referenceId);
			}
			else
			{
				throw new CliException($"Project {args.UnrealProjectName} not found");
			}
			return;
		}
		
		// If not, we need to check which options we have
		if (unrealProjects.Count == 1 || args.Quiet)
		{
			var onlyItem = unrealProjects.First();
			RemoveReferenceAndSetLinkedEngineProjects(args, linkedEngineProjects, onlyItem, referenceId);
			return;
		}
		
		var projectSelection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which [green]project[/] do you want to add the Replacement Type?")
				.AddChoices(unrealProjects.Select(item => item.GetProjectName()))
				.AddBeamHightlight()
		);
		
		var selectedProject = unrealProjects.First(item => item.GetProjectName() == projectSelection);
		RemoveReferenceAndSetLinkedEngineProjects(args, linkedEngineProjects, selectedProject, referenceId);
	}

	private static void RemoveReferenceAndSetLinkedEngineProjects(RemoveReplacementTypeCommandArgs args,
		EngineProjectData linkedEngineProjects, EngineProjectData.Unreal projectData, string referenceId)
	{
		var array = projectData.ReplacementTypeInfos ?? Array.Empty<ReplacementTypeInfo>();
		linkedEngineProjects.unrealProjectsPaths.Remove(projectData);
		projectData.ReplacementTypeInfos = array.Where(item => item.ReferenceId != referenceId).ToArray();
		linkedEngineProjects.unrealProjectsPaths.Add(projectData);
		args.ConfigService.SetLinkedEngineProjects(linkedEngineProjects);
	}

	private Task<string> GetReferenceId(RemoveReplacementTypeCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.ReferenceId))
		{
			return Task.FromResult(AnsiConsole.Prompt(
				new TextPrompt<string>("Please enter the Replacement [green]Reference Id[/]:").PromptStyle("green")));
		}
		return Task.FromResult(args.ReferenceId);
	}
}
