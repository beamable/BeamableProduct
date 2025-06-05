using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using cli.Dotnet;
using cli.Services;

namespace cli.OtelCommands;

using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class CollectorStatusCommandArgs : CommandArgs
{
	public bool watch;
}


public class CollectorStatusCommand : StreamCommand<CollectorStatusCommandArgs, CollectorStatus>
{
	public CollectorStatusCommand() : base("status", "Starts a stream of messages containing the status of the collector")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.watch = i); //Feels wrong getting this method from the Project suite, but duplicating it feels wrong too =/
	}

	public override async Task Handle(CollectorStatusCommandArgs args)
	{
		CollectorManager.AddDefaultCollectorHostAndPortFallback();
		
		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_HOST);
		
		var socket = CollectorManager.GetSocket(host, portNumber, BeamableZLoggerProvider.LogContext.Value);

		CollectorStatus currentStatus = await CollectorManager.IsCollectorRunning(socket, args.Lifecycle.Source.Token, BeamableZLoggerProvider.LogContext.Value);

		if (args.watch)
		{
			bool hasChanged = true;
			CollectorStatus lastStatus;
			while (!args.Lifecycle.Source.Token.IsCancellationRequested)
			{
				if (hasChanged)
				{
					LogStatus(currentStatus);
					SendResults(currentStatus);
				}

				lastStatus = currentStatus;
				currentStatus = await CollectorManager.IsCollectorRunning(socket, args.Lifecycle.Source.Token, BeamableZLoggerProvider.LogContext.Value);

				if (currentStatus.Equals(lastStatus))
				{
					hasChanged = false;
				}
				else
				{
					hasChanged = true;
				}
			}
		}
		else
		{
			LogStatus(currentStatus);
			SendResults(currentStatus);
		}
	}

	private void LogStatus(CollectorStatus currentStatus)//TODO improve this to be more visually interesting
	{
		if (!currentStatus.isRunning)
		{
			Log.Information($"Collector Status:");
			Log.Information($"There is no collector currently running\n\n");

		}
		else
		{
			Log.Information($"Collector Status:");
			Log.Information($"Is Running: {currentStatus.isRunning}");
			Log.Information($"Is Ready: {currentStatus.isReady}");
			Log.Information($"Process Id: {currentStatus.pid} \n\n");
		}
	}
}
