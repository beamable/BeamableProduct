using System.CommandLine;
using cli.Services;
using cli.Utils;
using Markdig.Extensions.TaskLists;
using Spectre.Console;

namespace cli.Commands.Project;


public class AddReplacementTypeCommandArgs : CommandArgs
{
	public string ReferenceId;
	public string EngineReplacementType;
	public string EngineOptionalReplacementType;
	public string EngineImport;
	public string UnrealProjectName;
}
public class AddReplacementTypeCommand : AppCommand<AddReplacementTypeCommandArgs>, IEmptyResult
{
	public AddReplacementTypeCommand() : base("add-replacement-type", "Add a replacement type for all Unreal linked projects")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("reference-id", "The reference Id (C# class/struct name) for the replacement"), (args, i) => args.ReferenceId = i);
		AddOption(new Option<string>("replacement-type", "The name of the Type to replaced with in Unreal auto-gen"), (args, i) => args.EngineReplacementType = i);
		AddOption(new Option<string>("optional-replacement-type", "The name of the Optional Type to replaced with in Unreal auto-gen"), (args, i) => args.EngineOptionalReplacementType = i);
		AddOption(new Option<string>("engine-import", "The full import for the replacement type to be used in Unreal auto-gen"), (args, i) => args.EngineImport = i);
		AddOption(new Option<string>("project-name", "The Unreal project name"), (args, i) => args.UnrealProjectName = i);
	}

	public override async Task Handle(AddReplacementTypeCommandArgs args)
	{
		var replacementType = new ReplacementTypeInfo()
		{
			ReferenceId = await GetReferenceId(args),
			EngineReplacementType = await GetEngineReplacementType(args),
			EngineOptionalReplacementType = await GetEngineOptionalReplacementType(args),
			EngineImport = await GetEngineImport(args)
		};

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
				linkedEngineProjects.unrealProjectsPaths.Remove(projectData);
				projectData.ReplacementTypeInfos = projectData.ReplacementTypeInfos.Append(replacementType).ToArray();
				linkedEngineProjects.unrealProjectsPaths.Add(projectData);
				args.ConfigService.SetLinkedEngineProjects(linkedEngineProjects);
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
			linkedEngineProjects.unrealProjectsPaths.Remove(onlyItem);
			onlyItem.ReplacementTypeInfos = onlyItem.ReplacementTypeInfos.Append(replacementType).ToArray();
			linkedEngineProjects.unrealProjectsPaths.Add(onlyItem);
			args.ConfigService.SetLinkedEngineProjects(linkedEngineProjects);
			return;
		}
		
		string projectSelection = GetProjectPrompt(unrealProjects);
		
		var selectedProject = unrealProjects.First(item => item.GetProjectName() == projectSelection);
		linkedEngineProjects.unrealProjectsPaths.Remove(selectedProject);
		selectedProject.ReplacementTypeInfos = selectedProject.ReplacementTypeInfos.Append(replacementType).ToArray();
		linkedEngineProjects.unrealProjectsPaths.Add(selectedProject);
		args.ConfigService.SetLinkedEngineProjects(linkedEngineProjects);
	}

	private static string GetProjectPrompt(HashSet<EngineProjectData.Unreal> unrealProjects)
	{
		var projectSelection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which [green]project[/] do you want to add the Replacement Type?")
				.AddChoices(unrealProjects.Select(item => item.GetProjectName()))
				.AddBeamHightlight()
		);
		return projectSelection;
	}

	private Task<string> GetReferenceId(AddReplacementTypeCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.ReferenceId))
		{
			return Task.FromResult(AnsiConsole.Prompt(
				new TextPrompt<string>("Please enter the Replacement [green]Reference Id[/]:").PromptStyle("green")));
		}
		return Task.FromResult(args.ReferenceId);
	}
	
	private Task<string> GetEngineReplacementType(AddReplacementTypeCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.EngineReplacementType))
		{
			return Task.FromResult(AnsiConsole.Prompt(
				new TextPrompt<string>("Please enter the [green]Engine Replacement Type[/]:").PromptStyle("green")));
		}
		return Task.FromResult(args.EngineReplacementType);
	}

	private Task<string> GetEngineOptionalReplacementType(AddReplacementTypeCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.EngineOptionalReplacementType))
		{
			return Task.FromResult(AnsiConsole.Prompt(
				new TextPrompt<string>("Please enter the [green]Engine Optional Replacement Type[/]:").PromptStyle("green")));
		}
		return Task.FromResult(args.EngineOptionalReplacementType);
	}
	
	private Task<string> GetEngineImport(AddReplacementTypeCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.EngineImport))
		{
			return Task.FromResult(AnsiConsole.Prompt(
				new TextPrompt<string>("Please enter the [green]Engine Import[/]:").PromptStyle("green")));
		}
		return Task.FromResult(args.EngineImport);
	}
}
