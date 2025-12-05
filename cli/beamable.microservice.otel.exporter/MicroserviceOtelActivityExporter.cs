using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using System.Diagnostics;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelActivityExporter : MicroserviceOtelExporter<Activity>
{
	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpTraceExporter _exporter;
	private bool _shouldRetry;

	private LimitedQueue<ActivityQueueData> _activitiesToFlush;

	public MicroserviceOtelActivityExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		var endpoint = $"{options.OtlpEndpoint}";
		if (options.Protocol == OtlpExportProtocol.HttpProtobuf)
		{
			endpoint = $"{endpoint}/v1/traces";
		}
		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri(endpoint),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpTraceExporter(_otlpOptions);
		_activitiesToFlush = new LimitedQueue<ActivityQueueData>(options.RetryQueueMaxSize);
		_shouldRetry = options.ShouldRetry;
	}

	public override ExportResult Export(in Batch<Activity> batch)
	{
		var resource = this.ParentProvider.GetResource();

		if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpTraceExporter>(_exporter, resource.Attributes.ToDictionary(), out _))
		{
			return ExportResult.Failure;
		}

		// this needs to happen before we try exporting it, because the OtlpExporter will just vanish with the records, even when failing
		List<Activity> records = new List<Activity>();
		foreach (var record in batch)
		{
			if (record.Status != ActivityStatusCode.Error)
			{
				continue;
			}
			records.Add(record);
		}

		var copyBatch = new Batch<Activity>(records.ToArray(), records.Count); // We do this because iterating over the circular buffer inside the Batch<Activity> actually removes the entries from the Batch

		var result = _exporter.Export(copyBatch);

		if (!_shouldRetry)
		{
			return result;
		}

		if (result == ExportResult.Success)
		{
			while (_activitiesToFlush.Count > 0)
			{
				var record = _activitiesToFlush.Dequeue();
				var lateRecords = new Batch<Activity>(record.Batch.ToArray(), record.Batch.Count);
				var recordResult = _exporter.Export(lateRecords);

				if (recordResult == ExportResult.Failure) // in case the export failed, we put the batch back in the queue and quit execution
				{
					_activitiesToFlush.Enqueue(record);
					return ExportResult.Failure;
				}
			}
		}
		else
		{
			_activitiesToFlush.Enqueue(new ActivityQueueData()
			{
				Batch = records
			});

			return ExportResult.Failure;
		}

		return result;
	}
}

public class ActivityQueueData
{
	public List<Activity> Batch;
}
