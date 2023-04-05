using Beamable.Common;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class AddUnrealClientOutputCommandArgs : CommandArgs
{
	public string path;
}

public class AddUnrealClientOutputCommand : AppCommand<AddUnrealClientOutputCommandArgs>
{
	public AddUnrealClientOutputCommand() : base("add-unreal-project", "Add a unreal project to this beamable cli project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "Relative path to the Unreal project"), (args, i) => args.path = i);
	}

	public override Task Handle(AddUnrealClientOutputCommandArgs args)
	{
		var workingDir = Directory.GetCurrentDirectory();
		var startingDir = args.path;
		var directory = args.path;

		var expectedUnrealParentDirectories = new string[]
		{
			".", // maybe the unreal project a child of the current folder...
			".." // or maybe the unreal project is a sibling of the current folder...
		}.Select(p => Path.Combine(startingDir, p)).ToArray();

		var defaultValues = GetUnrealProjectCandidates(expectedUnrealParentDirectories).ToList();
		if (defaultValues.Count == 1) // if there is only one detected file, offer to use that.
		{
			if (AnsiConsole.Confirm($"Automatically found{defaultValues[0]}. Add as unreal project?"))
			{
				args.ProjectService.AddUnrealProject(defaultValues[0]);
				return Task.CompletedTask;
			}
		}
		else if (defaultValues.Count > 0) // if there are many detected files, offer up a list of them
		{
			defaultValues.Add("continue");
			var selection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("Select the Unreal project to link, or continue to search manually")
					.AddChoices(defaultValues)
				);
			if (selection != "continue")
			{
				args.ProjectService.AddUnrealProject(selection);
				return Task.CompletedTask;
			}
		}

		while (!IsDirectoryUnrealEsque(directory))
		{
			var subDirs = Directory.GetDirectories(directory).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			subDirs.Add("..");
			var dirSelection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("This doesn't look like a Unreal project. Where is it from here?")
					.AddChoices(subDirs)
			);

			directory = Path.Combine(directory, dirSelection);
			directory = Path.GetRelativePath(workingDir, directory);
		}

		directory = Path.GetRelativePath(args.ConfigService.BaseDirectory, directory);

		if (startingDir != directory)
		{
			if (!AnsiConsole.Confirm($"Add {directory} as unreal project?"))
			{
				return Task.CompletedTask;
			}
		}

		args.ProjectService.AddUnrealProject(directory);
		return Task.CompletedTask;
	}

	IEnumerable<string> GetUnrealProjectCandidates(string[] paths)
	{
		foreach (var path in paths)
		{
			BeamableLogger.Log($"Looking in {path} for default unreal projects");
			var childPaths = Directory.GetDirectories(path);
			foreach (var childPath in childPaths)
			{
				BeamableLogger.Log($"-- looking at {childPath} ");

				if (IsDirectoryUnrealEsque(childPath))
				{
					BeamableLogger.Log($"-- I think that {childPath} is a unreal project");

					yield return childPath;
				}
			}
		}
	}

	bool IsDirectoryUnrealEsque(string path)
	{
		var subDirs = Directory.GetDirectories(path).ToList();
		subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

		var hasContent = subDirs.Contains("Content");
		var hasPlugins = subDirs.Contains("Plugins");
		var hasConfig = subDirs.Contains("Config");

		return hasContent && hasPlugins && hasConfig;
	}
}
