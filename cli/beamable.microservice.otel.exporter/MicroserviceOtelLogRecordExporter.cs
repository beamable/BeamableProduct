using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelLogRecordExporter : MicroserviceOtelExporter<LogRecord>
{

	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpLogExporter _exporter;
	private bool _shouldRetry;

	private Queue<LogRecordQueueData> _logRecordsToFlush; //TODO: Have a hard limit for this queue to not overflow with logs

	public MicroserviceOtelLogRecordExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{options.OtlpEndpoint}/v1/logs"),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpLogExporter(_otlpOptions);
		_logRecordsToFlush = new Queue<LogRecordQueueData>();
		_shouldRetry = options.ShouldRetry;
	}

	public override ExportResult Export(in Batch<LogRecord> batch)
	{
		var resource = this.ParentProvider.GetResource();

		if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpLogExporter>(_exporter, resource.Attributes.ToDictionary(), out _))
		{
			throw new Exception("Couldn't set resources data into OtlpExporter!");
		}

		// this needs to happen before we try exporting it, because the OtlpExporter will just vanish with the records, even when failing
		List<LogRecord> records = new List<LogRecord>();
		foreach (var record in batch)
		{
			records.Add(record);
		}

		//TODO have an environment variable to disable the retry, just try sending once

		var copyBatch = new Batch<LogRecord>(records.ToArray(), records.Count); // We do this because iterating over the circular buffer inside the Batch<LogRecord> actually removes the entries from the Batch

		var result = _exporter.Export(copyBatch);

		if (!_shouldRetry)
		{
			return result;
		}

		if (result == ExportResult.Success)
		{
			while (_logRecordsToFlush.Count > 0)
			{
				var record = _logRecordsToFlush.Dequeue();
				var lateRecords = new Batch<LogRecord>(record.Batch.ToArray(), record.Batch.Count);
				var recordResult = _exporter.Export(lateRecords);

				if (recordResult == ExportResult.Failure) // in case the export failed, we put the batch back in the queue and quit execution
				{
					_logRecordsToFlush.Enqueue(record);
					return ExportResult.Failure;
				}
			}
		}
		else
		{
			_logRecordsToFlush.Enqueue(new LogRecordQueueData()
			{
				Batch = records
			});

			return ExportResult.Failure;
		}

		return ExportResult.Success;
	}
}

public class LogRecordQueueData
{
	public List<LogRecord> Batch;
}
