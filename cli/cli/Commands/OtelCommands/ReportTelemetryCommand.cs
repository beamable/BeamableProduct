using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Util;
using beamable.otel.common;
using beamable.otel.exporter;
using beamable.otel.exporter.Serialization;
using Beamable.Server;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using System.CommandLine;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace cli.OtelCommands;

[Serializable]
public class ReportTelemetryCommandArgs : CommandArgs
{
	public string[] Paths;
}

public class ReportTelemetryResult
{
	public List<TelemetryReportStatus> AllStatus;
}

public class ReportTelemetryCommand : AtomicCommand<ReportTelemetryCommandArgs, ReportTelemetryResult>
{
	public override bool IsForInternalUse => true;

	public ReportTelemetryCommand() : base("report", "Sends custom telemetry data to CLI to be saved in file. Runs `beam otel push` to send this data to ClickHouse")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--paths", "All paths to files that contain custom log data to be later exported to clickhouse"),
			(args, i) => args.Paths = i);
	}

	public override Task<ReportTelemetryResult> GetResult(ReportTelemetryCommandArgs args)
	{

		if (args.Paths == null || args.Paths.Length == 0)
		{
			throw new CliException("Must have at least one path to a file to be exported");
		}

		List<TelemetryReportStatus> results = new List<TelemetryReportStatus>();

		foreach (var path in args.Paths)
		{
			var fileContent = File.ReadAllText(path);

			if (string.IsNullOrEmpty(fileContent))
			{
				Log.Warning("Report command was called, but file passed is empty.");
			}

			try
			{
				var cliOtelData = JsonConvert.DeserializeObject<CliOtelMessage>(fileContent);

				(List<SerializableLogRecord> serializedLogs, bool success) = GetSerializedLogs(cliOtelData.allLogs, out string errorSerializing);

				if (!success)
				{
					Log.Error(errorSerializing);

					results.Add(new TelemetryReportStatus()
					{
						Success = false,
						ErrorMessage = errorSerializing,
						FilePath = path
					});

					continue;
				}

				List<LogRecord> logRecords = serializedLogs.Select(LogRecordSerializer.DeserializeLogRecord).ToList();

				FileLogRecordExporter exporter = new FileLogRecordExporter(new FileExporterOptions()
				{
					ExportPath = args.ConfigService.ConfigTempOtelLogsDirectoryPath ?? ""
				});

				var objectDict = new Dictionary<string, object>()
				{
					{ Otel.ATTR_SOURCE, "cli" },
					{ Otel.ATTR_ENGINE_SOURCE, args.AppContext.EngineCalling},
					{ Otel.ATTR_ENGINE_SDK_VERSION, args.AppContext.EngineSdkVersion },
					{ Otel.ATTR_ENGINE_VERSION, args.AppContext.EngineVersion },
					{ Otel.ATTR_SDK_VERSION, BeamAssemblyVersionUtil.GetVersion<App>() }
				};

				if (!string.IsNullOrEmpty(args.AppContext.Cid))
				{
					objectDict[Otel.ATTR_CID] = args.AppContext.Cid;
				}
				if (!string.IsNullOrEmpty(args.AppContext.Pid))
				{
					objectDict[Otel.ATTR_PID] = args.AppContext.Pid;
				}

				if (!OtlpExporterResourceInjector.TrySetResourceField<FileLogRecordExporter>(exporter, objectDict, out var errorMessage))
				{
					var message = $"Failed to inject resource attributes into exporter. Message=[{errorMessage}]";
					Log.Error(message);
					results.Add(new TelemetryReportStatus()
					{
						Success = false,
						ErrorMessage = message,
						FilePath = path
					});

					continue;
				}

				var result = exporter.Export(new Batch<LogRecord>(logRecords.ToArray(), logRecords.Count));

				if (result == ExportResult.Failure)
				{
					var message = "Failed at exporting logs using the [FileLogExporter]";
					Log.Error(message);
					results.Add(new TelemetryReportStatus()
					{
						Success = false,
						ErrorMessage = message,
						FilePath = path
					});
				}
				else
				{
					results.Add(new TelemetryReportStatus()
					{
						Success = true,
						ErrorMessage = "",
						FilePath = path
					});
				}

			}
			catch (Exception ex)
			{
				var errorMessage =
					$"Failed to deserialize file at path=[{path}], it needs to be a json serialized format of the class [CliOtelMessage]";
				Log.Error(ex, errorMessage);
				results.Add(new TelemetryReportStatus()
				{
					Success = false,
					ErrorMessage = errorMessage,
					FilePath = path
				});
			}

		}

		var finalResult = new ReportTelemetryResult() { AllStatus = results };

		return Task.FromResult(finalResult);
	}


	private (List<SerializableLogRecord>, bool) GetSerializedLogs(List<CliOtelLogRecord> allLogs, out string errorMessage)
	{
		List<SerializableLogRecord> serializedLogs = new List<SerializableLogRecord>();

		foreach (var log in allLogs)
		{
			if (!Enum.TryParse(log.LogLevel, out LogLevel level))
			{
				errorMessage = $"Couldn't parse log level, got: [{log.LogLevel}]. Make sure that the string value for LogLevel is correct";
				return (new List<SerializableLogRecord>(), false);
			}

			serializedLogs.Add(new SerializableLogRecord()
			{
				Body = log.Body,
				CategoryName = "",
				FormattedMessage = log.Body,
				Timestamp = log.Timestamp,
				LogLevel = level,
				Exception = new ExceptionInfo()
				{
					Message = log.ExceptionMessage,
					StackTrace = log.ExceptionStackTrace
				},
				Attributes = log.Attributes
			});
		}

		errorMessage = "";
		return (serializedLogs, true);
	}
}
