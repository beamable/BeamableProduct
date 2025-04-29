using Beamable.Common;
using Beamable.Server;
using cli.Services;

namespace cli.OtelCommands;


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
		AssertEnvironmentVars();

		var processId = await CollectorManager.StartCollector(true, args.Lifecycle.Source, BeamableZLoggerProvider.LogContext.Value);

		Log.Information($"Collector with process id [{processId}] started successfully");
	}

	private void AssertEnvironmentVars()
	{
		var port = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_PORT");
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_HOST");

		if(string.IsNullOrEmpty(host))
		{
			throw new Exception("There is no host configured for the collector discovery");
		}

		var user = Environment.GetEnvironmentVariable("BEAM_CLICKHOUSE_USER");
		if(string.IsNullOrEmpty(user))
		{
			throw new Exception("There is no user configured for the collector startup");
		}

		var passd = Environment.GetEnvironmentVariable("BEAM_CLICKHOUSE_PASSWORD");
		if(string.IsNullOrEmpty(passd))
		{
			throw new Exception("There is no password configured for the collector startup");
		}
	}
}
