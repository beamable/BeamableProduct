namespace cli.OtelCommands;

public class GetBeamOtelConfigCommandArgs : CommandArgs
{

}

public class GetBeamOtelConfigCommand : AtomicCommand<GetBeamOtelConfigCommandArgs, OtelConfig>, ISkipManifest
{
	public GetBeamOtelConfigCommand() : base("config", "Retrieves Beam Open Telemetry configuration")
	{
	}

	public override void Configure()
	{
	}

	public override Task<OtelConfig> GetResult(GetBeamOtelConfigCommandArgs args)
	{
		return Task.FromResult(args.ConfigService.LoadOtelConfigFromFile());
	}
}
