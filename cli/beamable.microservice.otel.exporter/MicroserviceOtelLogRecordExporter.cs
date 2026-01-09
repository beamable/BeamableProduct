using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelLogRecordExporter : MicroserviceOtelExporter<LogRecord>
{

	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpLogExporter _exporter;
	private bool _shouldRetry;

	private LimitedQueue<LogRecordQueueData> _logRecordsToFlush;

	public MicroserviceOtelLogRecordExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		var endpoint = $"{options.OtlpEndpoint}";
		if (options.Protocol == OtlpExportProtocol.HttpProtobuf)
		{
			endpoint = $"{endpoint}/v1/logs";
		}

		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri(endpoint),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpLogExporter(_otlpOptions);
		_logRecordsToFlush = new LimitedQueue<LogRecordQueueData>(options.RetryQueueMaxSize);
		_shouldRetry = options.ShouldRetry;
	}

	public class LogRecordAccessors
	{
		private static readonly Type LogRecordSharedPoolType =
			typeof(LogRecord).Assembly.GetType("OpenTelemetry.Logs.LogRecordSharedPool");

		private static readonly Type LogRecordType =
			typeof(LogRecord).Assembly.GetType("OpenTelemetry.Logs.LogRecord");

		private static readonly MethodInfo ReturnMethod =
			LogRecordSharedPoolType?.GetMethod("Return", BindingFlags.Instance | BindingFlags.NonPublic);

		public static object GetCurrentPool()
		{
			var currentField = LogRecordSharedPoolType?.GetField("Current",
				BindingFlags.Instance | BindingFlags.NonPublic);
			return currentField?.GetValue(null);
		}

		public static object GetSource(LogRecord record)
		{
			var sourceField = LogRecordType?.GetField("Source",
				BindingFlags.Instance | BindingFlags.NonPublic);
			return sourceField?.GetValue(record) ?? "";
		}

		public static void Return(LogRecord logRecord)
		{
			var pool = GetCurrentPool();
			if (pool != null)
			{
				ReturnMethod?.Invoke(pool, new[] { logRecord });
			}
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(AddReference))]
		public static extern void AddReference(LogRecord c);
	}


	private void FreeRecords(List<LogRecord> allRecords)
	{
		foreach (var logRecord in allRecords)
		{
			if (LogRecordAccessors.GetSource(logRecord).ToString() == "FromSharedPool")
			{
				LogRecordAccessors.Return(logRecord);
			}
		}
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
			LogRecordAccessors.AddReference(record);
			records.Add(record);
		}

		var copyBatch = new Batch<LogRecord>(records.ToArray(), records.Count); // We do this because iterating over the circular buffer inside the Batch<LogRecord> actually removes the entries from the Batch

		var result = _exporter.Export(copyBatch);

		if (!_shouldRetry)
		{
			FreeRecords(records.ToList());
			return result;
		}

		if (result == ExportResult.Success)
		{
			FreeRecords(records.ToList());
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
				else
				{
					FreeRecords(record.Batch.ToList());
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
