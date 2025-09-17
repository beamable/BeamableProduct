using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using cli.Dotnet;
using cli.Utils;
using Spectre.Console;
using System.Diagnostics;

namespace cli.OtelCommands;

using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class CollectorStatusCommandArgs : CommandArgs
{
	public bool watch;
}

public class CollectorStatusResult
{
	public List<CollectorStatus> collectorsStatus;
}


public class CollectorStatusCommand : StreamCommand<CollectorStatusCommandArgs, CollectorStatusResult>, IResultSteam<ExtraStreamResultChannel, OtelFileStatus>
{
	
	private const int SECONDS_DELAY_TO_FILE_STATUS = 15;
	public CollectorStatusCommand() : base("ps", "Starts a stream of messages containing the status of the collector")
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

		CollectorStatusResult result = new CollectorStatusResult();

		// First take ~1s to check all running collectors 
		result.collectorsStatus = await CollectorManager.CheckAllRunningCollectors(socket, args.Lifecycle.Source.Token, BeamableZLoggerProvider.LogContext.Value);

		if (args.watch)
		{
			_ = Task.Run(async () =>
			{
				while (!args.Lifecycle.Source.Token.IsCancellationRequested)
				{
					var otelFileStatus = await GetOtelFileStatus(args.ConfigService.ConfigTempOtelDirectoryPath);
					this.SendResults<ExtraStreamResultChannel, OtelFileStatus>(otelFileStatus);
					await Task.Delay(TimeSpan.FromSeconds(SECONDS_DELAY_TO_FILE_STATUS));
				}
			});
			
			bool hasChanged = true;
			List<CollectorStatus> lastRunningCols;
			while (!args.Lifecycle.Source.Token.IsCancellationRequested)
			{
				if (hasChanged)
				{
					LogStatus(result.collectorsStatus);
					SendResults(result);
				}
				
				lastRunningCols = result.collectorsStatus.GetRange(0, result.collectorsStatus.Count);

				await Task.Delay(100);

				// Check if there is something running that is new or changed
				{
					var runningStatus =
						await CollectorManager.GetRunningCollectorMessage(socket, args.Lifecycle.Source.Token);

					if (runningStatus.foundCollector)
					{
						var collector = runningStatus.message;
						
						var index = result.collectorsStatus.FindIndex(s => s.pid == collector.pid);

						var collectorStatus = CollectorManager.GetCollectorStatus(collector);

						if (index >= 0)
						{
							result.collectorsStatus[index] = collectorStatus;
						}
						else
						{
							result.collectorsStatus.Add(collectorStatus);
						}
					}
				}

				// For all running collectors, check if their process is still running, if not then remove from list
				{
					for (int i = (result.collectorsStatus.Count - 1); i >= 0; i--)
					{
						var collector = result.collectorsStatus[i];
						var isProcessNotWorking = false;
						try
						{
							Process.GetProcessById(collector.pid);
						}
						catch
						{
							isProcessNotWorking = true;
						}

						if (isProcessNotWorking)
						{
							result.collectorsStatus.RemoveAt(i);
						}
					}
				}

				hasChanged = HaveStatusChanged(lastRunningCols, result.collectorsStatus);
			}
		}
		else
		{
			LogStatus(result.collectorsStatus);
			SendResults(result);
			var otelFileStatus = await GetOtelFileStatus(args.ConfigService.ConfigTempOtelDirectoryPath);
			this.SendResults<ExtraStreamResultChannel, OtelFileStatus>(otelFileStatus);
		}
	}
	
	private static async Task<OtelFileStatus> GetOtelFileStatus(string otelDirectory)
	{
		if (!Directory.Exists(otelDirectory))
		{
			return new OtelFileStatus();
		}

		DirectoryInfoUtils directoryInfo = DirectoryUtils.CalculateDirectorySize(otelDirectory);
		string lastPublishedFilePath = Path.Join(otelDirectory, PushTelemetryCommand.LAST_PUBLISH_OTEL_FILE_NAME);
		long lastPublished = 0;
		if(File.Exists(lastPublishedFilePath))
		{
			string fileText = await File.ReadAllTextAsync(lastPublishedFilePath);
			long.TryParse(fileText, out lastPublished);
		}

		var otelFileStatus= new OtelFileStatus()
		{
			FileCount = directoryInfo.FileCount, FolderSize = directoryInfo.Size, LastPublishTimestamp = lastPublished
		};
		return otelFileStatus;
	}

	private bool HaveStatusChanged(List<CollectorStatus> lastStatusList, List<CollectorStatus> currentStatusList)
	{
		if (lastStatusList.Count != currentStatusList.Count)
		{
			return true;
		}

		foreach (CollectorStatus status in lastStatusList)
		{
			CollectorStatus currentMatch = currentStatusList.FirstOrDefault(s => s.pid == status.pid);
			if (currentMatch == null)
			{
				return true;
			}
			
			if (status.isReady != currentMatch.isReady)
			{
				return true;
			}
		}

		return false;
	}

	private void LogStatus(List<CollectorStatus> currentStatusList)
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
