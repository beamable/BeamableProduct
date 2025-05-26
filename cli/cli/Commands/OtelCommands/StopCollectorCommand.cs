using Beamable.Server;
using cli.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace cli.OtelCommands;


[Serializable]
public class StopCollectorCommandArgs : CommandArgs
{
}


public class StopCollectorCommand : AppCommand<StopCollectorCommandArgs>
{
	public StopCollectorCommand() : base("stop", "Stops the collector running process")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(StopCollectorCommandArgs args)
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

		Log.Information($"Starting listening to otel collector in port [{portNumber}]...");

		var socket = CollectorManager.GetSocket(host, portNumber);

		var status = await CollectorManager.IsCollectorRunning(socket, args.Lifecycle.Source.Token);

		if (status.isRunning)
		{
			try
			{
				Log.Information($"Stopping process id: {status.pid}");
				var proc = Process.GetProcessById(status.pid);
				proc.Kill();
			}
			catch (Exception ex)
			{
				Log.Verbose($"killing process=[{status.pid}] results in error=[{ex.Message}]");
			}
		}
		else
		{
			Log.Information("There is no collector running right now.");
		}
	}
}
