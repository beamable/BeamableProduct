using Beamable.Common.BeamCli.Contracts;
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
	public string Path;
}

public class ReportTelemetryCommand : AppCommand<ReportTelemetryCommandArgs>
{
	public override bool IsForInternalUse => true;

	public ReportTelemetryCommand() : base("report", "Sends custom telemetry data to CLI to be saved in file. Runs `beam otel push` to send this data to ClickHouse")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--path", "The path for the file with logs to be saved and later pushed to Clickhouse"),
			(args, i) => args.Path = i);
	}

	public override Task Handle(ReportTelemetryCommandArgs args)
	{
		var fileContent = File.ReadAllText(args.Path);

		if (string.IsNullOrEmpty(fileContent))
		{
			Log.Warning("Report command was called, but file passed is empty.");
		}

		try
		{
			var cliOtelData = JsonConvert.DeserializeObject<CliOtelMessage>(fileContent);
			List<SerializableLogRecord> serializedLogs = new List<SerializableLogRecord>();

			foreach (var log in cliOtelData.allLogs)
			{
				if (!Enum.TryParse(log.LogLevel, out LogLevel level))
				{
					Log.Error($"Couldn't parse log level, got: [{log.LogLevel}]. Make sure that the string value for LogLevel is correct");
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
			};

			//TODO fix this
			// if (!string.IsNullOrEmpty(args.))
			// {
			// 	objectDict[Otel.ATTR_CID] = ctx.Cid;
			// }
			// if (!string.IsNullOrEmpty(ctx.Pid))
			// {
			// 	dict[Otel.ATTR_PID] = ctx.Pid;
			// }

			//TODO also fix this, need to work for the FileLogExporter instead of the otlp one
			// if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpTraceExporter>(otlpTraceExporter, objectDict, out var errorMessage))
			// {
			// 	Log.Error($"Failed to inject resource attributes into exporter. Message=[{errorMessage}]");
			// }


			var result = exporter.Export(new Batch<LogRecord>(logRecords.ToArray(), logRecords.Count));

			if (result == ExportResult.Failure)
			{
				Log.Error("Failed at exporting logs using the [FileLogExporter]");
			}
			else
			{
				Log.Information("All logs were exported to files!");
			}

		}
		catch (Exception ex)
		{
			Log.Error(ex, $"Failed to deserialize file at path=[{args.Path}], it needs to be a json serialized format of the class [CliOtelMessage]");
		}


		return Task.CompletedTask;
	}
}
