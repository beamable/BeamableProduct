using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelLogRecordExporter : MicroserviceOtelExporter<LogRecord>
{

	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpLogExporter _exporter;

	public MicroserviceOtelLogRecordExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{options.OtlpEndpoint}/v1/logs"),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpLogExporter(_otlpOptions);
	}

	public override ExportResult Export(in Batch<LogRecord> batch)
	{
		var resource = this.ParentProvider.GetResource();

		if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpLogExporter>(_exporter, resource.Attributes.ToDictionary(), out _))
		{
			return ExportResult.Failure;
		}

		var result = _exporter.Export(batch);
		return result;
	}
}
