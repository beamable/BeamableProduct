using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelMetricExporter : MicroserviceOtelExporter<Metric>
{
	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpMetricExporter _exporter;

	public MicroserviceOtelMetricExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{options.OtlpEndpoint}/v1/metrics"),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpMetricExporter(_otlpOptions);
	}

	public override ExportResult Export(in Batch<Metric> batch)
	{
		var resource = this.ParentProvider.GetResource();

		if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpMetricExporter>(_exporter, resource.Attributes.ToDictionary(), out _))
		{
			return ExportResult.Failure;
		}

		var result = _exporter.Export(batch);
		return result;
	}
}
