using Beamable.Common;
using Beamable.Server;
using cli.Services;
using System.Net;

namespace cli.OtelCommands;
using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class StartCollectorCommandArgs : CommandArgs
{
}

public class CollectorItemToDownload
{
	public string fileName;
	public string downloadUrl;
	public string filePath;
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
		AssertEnvironmentVars();//TODO this requirement is just while we don't have a way to get credentials from beamo

		var basePath = CollectorManager.GetCollectorBasePathForCli();
		var processId = await CollectorManager.StartCollector(basePath, true, true, args.Lifecycle.Source, BeamableZLoggerProvider.LogContext.Value);

		Log.Information($"Collector with process id [{processId}] started successfully");
	}

	private void AssertEnvironmentVars()
	{
		CollectorManager.AddDefaultCollectorHostAndPortFallback();
		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_HOST);

		if(string.IsNullOrEmpty(host))
		{
			throw new Exception("There is no host configured for the collector discovery");
		}

		var user = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_USERNAME);
		if(string.IsNullOrEmpty(user))
		{
			throw new Exception("There is no user configured for the collector startup");
		}

		var passd = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_PASSWORD);
		if(string.IsNullOrEmpty(passd))
		{
			throw new Exception("There is no password configured for the collector startup");
		}
		
		var chHost = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_ENDPOINT);
		if(string.IsNullOrEmpty(chHost))
		{
			throw new Exception("There is no clickhouse endpoint configured for the collector startup");
		}
	}
}
