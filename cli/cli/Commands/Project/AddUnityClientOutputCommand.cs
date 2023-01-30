using Beamable.Common;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class AddUnityClientOutputCommandArgs : CommandArgs
{
	public string path;
	public bool interactive = true;
}

public class AddUnityClientOutputCommand : AppCommand<AddUnityClientOutputCommandArgs>
{
	public AddUnityClientOutputCommand() : base("add-unity-project", "add a unity project to this beamable cli project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "the relative path to the Unity project"), (args, i) => args.path = i);
		// AddOption(new Option<bool>("--no-interaction", "if"), (args, i) => args.path = i);
	}

	public override Task Handle(AddUnityClientOutputCommandArgs args)
	{
		var workingDir = Directory.GetCurrentDirectory();
		var directory = Path.Combine(workingDir, args.path);
		var startingDir = directory = Path.GetRelativePath(workingDir, directory);

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

	bool IsDirectoryUnityEsque(string path)
	{
		var subDirs = Directory.GetDirectories(path).ToList();
		subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

		var hasAssets = subDirs.Contains("Assets");
		var hasPackages = subDirs.Contains("Packages");

		return hasAssets && hasPackages;
	}
}
