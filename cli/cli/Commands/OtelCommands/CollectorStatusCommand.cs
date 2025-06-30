using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using cli.Dotnet;
using Spectre.Console;

namespace cli.OtelCommands;

using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class CollectorStatusCommandArgs : CommandArgs
{
	public bool watch;
}


public class CollectorStatusCommand : StreamCommand<CollectorStatusCommandArgs, List<CollectorStatus>>
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
		
		var socket = CollectorManager.GetSocket(portNumber, BeamableZLoggerProvider.LogContext.Value);

		var currentRunningCols = await CollectorManager.CheckAllRunningCollectors(socket, args.Lifecycle.Source.Token,
			BeamableZLoggerProvider.LogContext.Value);;

		if (args.watch)
		{
			bool hasChanged = true;
			List<CollectorStatus> lastRunningCols;
			while (!args.Lifecycle.Source.Token.IsCancellationRequested)
			{
				if (hasChanged)
				{
					LogStatus(currentRunningCols);
					SendResults(currentRunningCols);
				}

				lastRunningCols = currentRunningCols;
				currentRunningCols = await CollectorManager.CheckAllRunningCollectors(socket, args.Lifecycle.Source.Token,
					BeamableZLoggerProvider.LogContext.Value);

				if (HaveStatusChanged(lastRunningCols, currentRunningCols))
				{
					hasChanged = true;
				}
				else
				{
					hasChanged = false;
				}
			}
		}
		else
		{
			LogStatus(currentRunningCols);
			SendResults(currentRunningCols);
		}
	}

	private bool HaveStatusChanged(List<CollectorStatus> lastStatusList, List<CollectorStatus> currentStatusList)
	{
		if (lastStatusList.Count != currentStatusList.Count)
		{
			return true;
		}

		foreach (CollectorStatus status in lastStatusList)
		{
			var currentMatch = currentStatusList.FirstOrDefault(s => s.pid == status.pid);
			if (currentMatch == null)
			{
				return true;
			}
		}

		return false;
	}

	private void LogStatus(List<CollectorStatus> currentStatusList)//TODO improve this to be more visually interesting
	{
		if (currentStatusList.Count == 0)
		{
			Log.Information($"Collector Status:");
			Log.Information($"There is no collector currently running\n\n");

		}
		else
		{
			var columnNameStyle = new Style(Color.SlateBlue1);

			var table = new Table();
			var processIdColumn = new TableColumn(new Markup("ProcessId", columnNameStyle));
			var versionColumn = new TableColumn(new Markup("Version", columnNameStyle));
			var isReadyColumn = new TableColumn(new Markup("Ready for data", columnNameStyle));
			var otlpEndpointColumn = new TableColumn(new Markup("OTLP Endpoint", columnNameStyle));

			table.AddColumn(processIdColumn).AddColumn(versionColumn).AddColumn(isReadyColumn).AddColumn(otlpEndpointColumn);
			foreach (var status in currentStatusList)
			{
				var processIdMarkup = new Markup($"[green]{status.pid}[/]");
				var versionMarkup = new Markup($"{status.version}");
				var isReadyMarkup = new Markup(status.isReady ? "[green]True[/]" : "[red]False[/]");
				var otlpEndpointMarkup = new Markup($"{status.otlpEndpoint}");

				table.AddRow(new TableRow(new[] { processIdMarkup, versionMarkup, isReadyMarkup, otlpEndpointMarkup}));
			}
			AnsiConsole.Write(table);
		}
	}
}
