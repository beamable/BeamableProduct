using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace cli.OtelCommands;

public class SetBeamOtelConfigCommandArgs : CommandArgs
{
	public string logLevel;
	public string maxSize;
}

public class SetBeamOtelConfigCommandResults
{

}

public class SetBeamOtelConfigCommand : AtomicCommand<SetBeamOtelConfigCommandArgs, SetBeamOtelConfigCommandResults>, ISkipManifest
{
	public SetBeamOtelConfigCommand() : base("set-config", "Sets the Beam CLI otel configuration")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("cli-log-level", "The minimum Open Telemetry LogLevel to be sent to Clickhouse, this needs to be a valid LogLevel converted to string value"), (arg, i) => arg.logLevel = i);
		AddArgument(new Argument<string>("cli-telemetry-max-size", "The maximum size in bytes for saved Otel data"), (arg, i) => arg.maxSize = i);
	}

	public override Task<SetBeamOtelConfigCommandResults> GetResult(SetBeamOtelConfigCommandArgs args)
	{
		var otelConfig = new OtelConfig()
		{
			BeamCliTelemetryLogLevel = args.logLevel,
			BeamCliTelemetryMaxSize = long.Parse(args.maxSize)
		};

		args.ConfigService.SaveOtelConfigToFile(otelConfig);
		return Task.FromResult(new SetBeamOtelConfigCommandResults());
	}
}
