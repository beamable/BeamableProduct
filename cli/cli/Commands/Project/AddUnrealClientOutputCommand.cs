using Beamable.Common;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;


public class AddUnrealClientOutputCommand : AppCommand<AddProjectClientOutputCommandArgs>
{
	public AddUnrealClientOutputCommand() : base("add-unreal-project", "Add a unreal project to this beamable cli project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "Relative path to the Unreal project"), (args, i) => args.path = i);
	}

	public override Task Handle(AddProjectClientOutputCommandArgs args)
	{
		var unrealProjectClient = new ProjectClientHelper<UnrealProjectClient>();
		var workingDir = Directory.GetCurrentDirectory();
		var startingDir = args.path;
		var directory = args.path;

		var expectedUnrealParentDirectories = new[]
		{
			".", // maybe the unreal project a child of the current folder...
			".." // or maybe the unreal project is a sibling of the current folder...
		}.Select(p => Path.Combine(startingDir, p)).ToArray();

		var status = unrealProjectClient.SuggestProjectClientTypeCandidates(expectedUnrealParentDirectories, args);
		if (status) return Task.CompletedTask;

		unrealProjectClient.FindProjectClientInDirectory(workingDir, ref directory);

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
}
