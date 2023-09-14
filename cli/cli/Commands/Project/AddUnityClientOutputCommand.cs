using Beamable.Common;
using cli.Utils;
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
		var unityProjectClient = new ProjectClientHelper<UnityProjectClient>();
		var workingDir = Directory.GetCurrentDirectory();
		var startingDir = args.path;
		var directory = args.path;

		var expectedUnityParentDirectories = new[]
		{
			".", // maybe the unity project a child of the current folder...
			".." // or maybe the unity project is a sibling of the current folder...
		}.Select(p => Path.Combine(startingDir, p)).ToArray();

		var status = unityProjectClient.SuggestProjectClientTypeCandidates(expectedUnityParentDirectories, args);
		if (status) return Task.CompletedTask;

		unityProjectClient.FindProjectClientInDirectory(workingDir, ref directory);

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
}
