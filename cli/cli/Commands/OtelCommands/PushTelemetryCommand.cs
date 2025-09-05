using Beamable.Common.Api;
using beamable.otel.exporter;
using Beamable.Server;
using OpenTelemetry;
using System.CommandLine;

namespace cli.OtelCommands;

[Serializable]
public class PushTelemetryCommandArgs : CommandArgs
{
	public string Endpoint;
	public string ProcessId;
}

public class PushTelemetryCommand : AppCommand<PushTelemetryCommandArgs>, IEmptyResult
{
	public const string LAST_PUBLISH_OTEL_FILE_NAME = "last-publish.txt";

	public PushTelemetryCommand() : base("push", "Pushes local telemetry data saved through the BeamableExporter to a running collector. This uses the Open Telemetry OTLP exporter to push telemetry from files to a running collector using Http protocol")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--endpoint", "The endpoint to which the telemetry data should be exported"),
			(args, i) => args.Endpoint = i);
		AddOption(new Option<string>("--process-id", () => string.Empty, "Defines the process Id that called this method. If is not passed a new process ID will be generated"), 
			(args, id) => args.ProcessId = id);
	}

	public override async Task Handle(PushTelemetryCommandArgs args)
	{
		int processId = !string.IsNullOrEmpty(args.ProcessId) && int.TryParse(args.ProcessId, out int processIdInt)? processIdInt : Environment.ProcessId;
		bool couldLockFile = await args.ProcessFileLocker.LockFile(OtelCommand.OTEL_COMMANDS_LOCK_FILE, processId);
		if (!couldLockFile)
		{
			Log.Information($"Couldn't run operation because lock file {OtelCommand.OTEL_COMMANDS_LOCK_FILE} is locked by another process");
			return;
		}

		try
		{
			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelLogsDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry logs path");
			}

			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelTracesDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry traces path");
			}

			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelMetricsDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry metrics path");
			}

			string endpointToUse;

			if (!string.IsNullOrEmpty(args.Endpoint))
			{
				endpointToUse = args.Endpoint;
			}else
			{
				CancellationTokenSource tokenSource = new CancellationTokenSource();

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

				var basePath = CollectorManager.GetCollectorBasePathForCli();
				var collectorStatus = await CollectorManager.StartCollectorAndWait(basePath, true, true, tokenSource, BeamableZLoggerProvider.LogContext.Value);
				endpointToUse = $"http://{collectorStatus.otlpEndpoint}";
			}

			{ // Sending deserialized telemetry data through Otlp exporter
				Log.Verbose($"Exporting logs to endpoint: {endpointToUse}");
				var logsResult = FileOtlpExporter.ExportLogs(args.ConfigService.ConfigTempOtelLogsDirectoryPath, endpointToUse, out string errorLogMsg);

				if (logsResult == ExportResult.Failure)
				{
					throw new CliException(
						"Failed to export logs. Make sure there is a collector receiving data in the correct endpoint." +
						$"Error=[{errorLogMsg}]");
				}

				Log.Verbose($"Exporting traces to endpoint: {endpointToUse}");
				var traceResult = FileOtlpExporter.ExportTraces(args.ConfigService.ConfigTempOtelTracesDirectoryPath, endpointToUse, out string errorTraceMsg);

				if (traceResult == ExportResult.Failure)
				{
					throw new CliException("Error while trying to export traces to collector. Make sure you have a collector running and expecting data." +
					                       $"Error=[{errorTraceMsg}]");
				}

				Log.Verbose($"Exporting metrics to endpoint: {endpointToUse}");
				var metricsResult = FileOtlpExporter.ExportMetrics(args.ConfigService.ConfigTempOtelMetricsDirectoryPath, endpointToUse, out string errorMetricMsg);

				if (metricsResult == ExportResult.Failure)
				{
					throw new CliException("Error while trying to export metrics to collector. Make sure you have a collector running and expecting data." +
					                       $"Error=[{errorMetricMsg}]");
				}
				await File.WriteAllTextAsync(Path.Join(args.ConfigService.ConfigTempOtelDirectoryPath, LAST_PUBLISH_OTEL_FILE_NAME), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
				Log.Information("Telemetry data was successfully exported!");
			}
		}
		finally
		{
			await args.ProcessFileLocker.UnlockFile(OtelCommand.OTEL_COMMANDS_LOCK_FILE, processId);
		}
	}
}
