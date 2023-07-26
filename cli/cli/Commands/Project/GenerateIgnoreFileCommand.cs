using System.CommandLine;

namespace cli.Commands.Project;

public class GenerateIgnoreFileCommand : AppCommand<GenerateIgnoreFileCommandArgs>
{
	public GenerateIgnoreFileCommand() : base("generate-ignore-file", "Generate an ignore file in .beamable folder for given VCS")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<Vcs>("type", "Which VCS to generate the ignore file for"), (args, i) => args.type = i);
	}

	public override Task Handle(GenerateIgnoreFileCommandArgs args)
	{
		args.ConfigService.CreateIgnoreFile(args.type, true);
		return Task.CompletedTask;
	}
}

public class GenerateIgnoreFileCommandArgs : CommandArgs
{
	public Vcs type;
}
