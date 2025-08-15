using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Server;

namespace cli.OtelCommands;
using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class StartCollectorCommandArgs : CommandArgs
{
}

public class StartCollectorCommand : AppCommand<StartCollectorCommandArgs>
{
	public StartCollectorCommand() : base("start", "Starts the collector process")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(StartCollectorCommandArgs args)
	{
		await AssertEnvironmentVars(args);

		var basePath = CollectorManager.GetCollectorBasePathForCli();
		var processId = await CollectorManager.StartCollector(basePath, true, true, args.Lifecycle.Source, BeamableZLoggerProvider.LogContext.Value);

		Log.Information($"Collector with process id [{processId}] started successfully");
	}

	private async Promise AssertEnvironmentVars(StartCollectorCommandArgs args)
	{
		CollectorManager.AddDefaultCollectorHostAndPortFallback();
		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int _))
		{
			throw new Exception("Invalid value for port");
		}

		try
		{
			var res = await args.OtelApi.GetOtelAuthWriterConfig();
			CollectorManager.AddAuthEnvironmentVars(res);
		}
		catch (RequesterException ex)
		{
			throw new CliException(
				message: $"An error happened while trying to get otel credentials from Beamo. Message=[{ex.Message}] StackTrace=[{ex.StackTrace}]");
		}
	}
}
