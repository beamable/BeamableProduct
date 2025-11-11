using Beamable.Common;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class UnrealAddProjectClientOutputCommandArgs : AddProjectClientOutputCommandArgs
{
	public string msModuleName;
	public bool? msModulePublicPrivate;
	public string bpModuleName;
	public bool? bpModulePublicPrivate;
}

public class AddUnrealClientOutputCommand : AppCommand<UnrealAddProjectClientOutputCommandArgs>
{
	public AddUnrealClientOutputCommand() : base("add-unreal-project", "Add a unreal project to this beamable cli project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "Relative path to the Unreal project"), (args, i) => args.path = i);
		AddOption(new Option<string>("--ms-module-name", "Name of a Runtime module in your project. This is where we'll add the MS's Client Subsystem's header/cpp files"), (args, i) => args.msModuleName = i);
		AddOption(new Option<bool?>("--ms-public-private", "Whether the Runtime MS Module splits files into Public/Private folders"), (args, i) => args.msModulePublicPrivate = i);
		AddOption(new Option<string>("--bp-module-name", "Name of a UncookedOnly module in your project. This is where we'll add the MS's BP nodes header/cpp files"), (args, i) => args.bpModuleName = i);
		AddOption(new Option<bool?>("--bp-public-private", "Whether the UncookedOnly module for BP Nodes splits files into Public/Private folders"), (args, i) => args.bpModulePublicPrivate = i);
	}

	public override Task Handle(UnrealAddProjectClientOutputCommandArgs args)
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

		directory = Path.GetRelativePath(args.ConfigService.BeamableWorkspace, directory);
		if (startingDir != directory)
		{
			if (!AnsiConsole.Confirm($"Add {directory} as unreal project?"))
			{
				return Task.CompletedTask;
			}
		}

		unrealProjectClient.AddProject(directory, args);
		return Task.CompletedTask;
	}
}
