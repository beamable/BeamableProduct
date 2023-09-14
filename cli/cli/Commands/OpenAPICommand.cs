namespace cli;

public class OpenAPICommandArgs : CommandArgs
{
	// TODO: add filter options...
}

public class OpenAPICommand : AppCommand<OpenAPICommandArgs>, IStandaloneCommand
{
	public OpenAPICommand() : base("oapi", "Commands that integrate the Beamable API and open API")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(OpenAPICommandArgs args)
	{
		return Task.CompletedTask;

	}
}
