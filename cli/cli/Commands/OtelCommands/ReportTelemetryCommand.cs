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
	public List<string> Paths;
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
		AddOption(new Option<List<string>>("--paths", "All paths to files that contain custom log data to be later exported to clickhouse"),
			(args, i) => args.Paths = i);
	}

	public override Task<ReportTelemetryResult> GetResult(ReportTelemetryCommandArgs args)
	{
		//Just for testing purposes
		{
			for (int i = 0; i < args.Paths.Count; i++)
			{
				var logsTest = new CliOtelMessage()
				{
					EngineVersion = "3.0",
					SdkVersion = $"2022.13f.{i}",
					Source = "unity",
					allLogs = new List<CliOtelLogRecord>()
				};
				logsTest.allLogs.Add(new CliOtelLogRecord()
				{
					Body = $"{i} - The first log here!",
					ExceptionMessage = "",
					ExceptionStackTrace = "",
					LogLevel = "Information",
					Timestamp = "2025-08-26T15:25:46.2695590Z",
					Attributes = new Dictionary<string, string>()
				});
				logsTest.allLogs.Add(new CliOtelLogRecord()
				{
					Body = $"{i} - The second log here!",
					ExceptionMessage = "",
					ExceptionStackTrace = "",
					LogLevel = "Information",
					Timestamp = "2025-08-26T15:25:48.2695590Z",
					Attributes = new Dictionary<string, string>()
				});

				File.WriteAllText(args.Paths[i], JsonConvert.SerializeObject(logsTest));
			}

		}

		if (args.Paths.Count == 0)
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
					{ Otel.ATTR_SOURCE, cliOtelData.Source },
					{ Otel.ATTR_SOURCE_VERSION, cliOtelData.SdkVersion },
					{ Otel.ATTR_SOURCE_ENGINE_VERSION, cliOtelData.EngineVersion },
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
				}
			});
		}

		errorMessage = "";
		return (serializedLogs, true);
	}
}
