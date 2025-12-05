namespace cli.OtelCommands;

public class GetBeamOtelConfigCommandArgs : CommandArgs
{

}

[Serializable]
public class GetBeamOtelConfigCommandResult
{
	public string BeamCliTelemetryLogLevel;
	public long BeamCliTelemetryMaxSize;
	public bool BeamCliAllowTelemetry;
}

public class GetBeamOtelConfigCommand : AtomicCommand<GetBeamOtelConfigCommandArgs, GetBeamOtelConfigCommandResult>, ISkipManifest
{
	public GetBeamOtelConfigCommand() : base("config", "Retrieves Beam Open Telemetry configuration")
	{
	}

	public override void Configure()
	{
	}

	public override Task<GetBeamOtelConfigCommandResult> GetResult(GetBeamOtelConfigCommandArgs args)
	{
		var otelConfig = args.ConfigService.LoadOtelConfigFromFile();

		return Task.FromResult(new GetBeamOtelConfigCommandResult()
		{
			BeamCliTelemetryLogLevel = otelConfig.BeamCliTelemetryLogLevel,
			BeamCliTelemetryMaxSize = otelConfig.BeamCliTelemetryMaxSize,
			BeamCliAllowTelemetry = otelConfig.BeamCliAllowTelemetry
		});
	}
}
