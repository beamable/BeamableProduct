using Beamable.Common.Semantics;
using System.CommandLine;

namespace cli.Commands.Project;

public class CreateCommonLibraryArgs : SolutionCommandArgs
{
	public string OutputPath;
}

public class NewCommonLibraryCommand : AppCommand<CreateCommonLibraryArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewCommonLibraryCommand(InitCommand initCommand) : base("common-lib", "Create common library project that later can be connected to the services")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddOption(new AutoInitFlag(), (args, b) => args.AutoInit = b);
		AddArgument(new Argument<ServiceName>("name", "The name of the new library project"),
			(args, i) => args.ProjectName = i);
		SolutionCommandArgs.ConfigureSolutionFlag(this);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		AddOption(new Option<string>("--output-path", "The path where the project is going to be created"),
			(args, i) => args.OutputPath = i);
	}

	public override async Task Handle(CreateCommonLibraryArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);
		var path = Path.Combine(args.OutputPath, args.ProjectName);
		await args.ProjectService.CreateCommonProject(args.ProjectName.Value, path, args.SpecifiedVersion);
	}
}
