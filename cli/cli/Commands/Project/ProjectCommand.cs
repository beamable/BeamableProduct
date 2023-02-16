
namespace cli.Dotnet;

public class ProjectCommandArgs : CommandArgs { }

public class ProjectCommand : AppCommand<ProjectCommandArgs>
{
	public ProjectCommand() : base(
		"project", 
		"Commands that relate to a standalone Beamable project")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task Handle(ProjectCommandArgs args)
	{
		return Task.CompletedTask;
	}
}
