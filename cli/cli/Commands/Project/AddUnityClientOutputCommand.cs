using Beamable.Common;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class AddUnityClientOutputCommandArgs : CommandArgs
{
	public string path;
}

public class AddUnityClientOutputCommand : AppCommand<AddUnityClientOutputCommandArgs>
{
	public AddUnityClientOutputCommand() : base("add-unity-project", "Add a unity project to this beamable cli project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "Relative path to the Unity project"), (args, i) => args.path = i);
	}

	public override Task Handle(AddUnityClientOutputCommandArgs args)
	{
		var workingDir = Directory.GetCurrentDirectory();
		var startingDir = args.path;
		var directory = args.path;

		var expectedUnityParentDirectories = new string[]
		{
			".", // maybe the unity project a child of the current folder...
			".." // or maybe the unity project is a sibling of the current folder...
		}.Select(p => Path.Combine(startingDir, p)).ToArray();
		
		var defaultValues = GetUnityProjectCandidates(expectedUnityParentDirectories).ToList();
		if (defaultValues.Count == 1) // if there is only one detected file, offer to use that.
		{
			if (AnsiConsole.Confirm($"Automatically found{defaultValues[0]}. Add as unity project?"))
			{
				args.ProjectService.AddUnityProject(defaultValues[0]);
				return Task.CompletedTask;
			}
		} else if (defaultValues.Count > 0) // if there are many detected files, offer up a list of them
		{
			defaultValues.Add("continue");
			var selection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("Select the Unity project to link, or continue to search manually")
					.AddChoices(defaultValues)
				);
			if (selection != "continue")
			{
				args.ProjectService.AddUnityProject(selection);
				return Task.CompletedTask;
			}
		}
		
		while (!IsDirectoryUnityEsque(directory))
		{
			var subDirs = Directory.GetDirectories(directory).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			subDirs.Add("..");
			var dirSelection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("This doesn't look like a Unity project. Where is it from here?")
					.AddChoices(subDirs)
			);
			
			directory = Path.Combine(directory, dirSelection);
			directory = Path.GetRelativePath(workingDir, directory);
		}

		directory = Path.GetRelativePath(args.ConfigService.BaseDirectory, directory);

		if (startingDir != directory)
		{
			if (!AnsiConsole.Confirm($"Add {directory} as unity project?"))
			{
				return Task.CompletedTask;
			}
		}

		args.ProjectService.AddUnityProject(directory);
		return Task.CompletedTask;
	}

	IEnumerable<string> GetUnityProjectCandidates(string[] paths)
	{
		foreach (var path in paths)
		{
			BeamableLogger.Log($"Looking in {path} for default unity projects");
			var childPaths = Directory.GetDirectories(path);
			foreach (var childPath in childPaths)
			{
				BeamableLogger.Log($"-- looking at {childPath} ");

				if (IsDirectoryUnityEsque(childPath))
				{
					BeamableLogger.Log($"-- I think that {childPath} is a unity projet");

					yield return childPath;
				}
			}
		}
	}
	
	bool IsDirectoryUnityEsque(string path)
	{
		var subDirs = Directory.GetDirectories(path).ToList();
		subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

		var hasAssets = subDirs.Contains("Assets");
		var hasPackages = subDirs.Contains("Packages");
		var hasSettings = subDirs.Contains("ProjectSettings");

		return hasAssets && hasPackages && hasSettings;
	}
}
