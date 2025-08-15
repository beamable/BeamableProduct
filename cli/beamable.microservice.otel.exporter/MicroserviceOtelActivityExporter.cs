using beamable.otel.common;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using System.Diagnostics;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelActivityExporter : MicroserviceOtelExporter<Activity>
{
	private readonly OtlpExporterOptions _otlpOptions;
	private OtlpTraceExporter _exporter;

	public MicroserviceOtelActivityExporter(MicroserviceOtelExporterOptions options) : base(options)
	{
		_otlpOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{options.OtlpEndpoint}/v1/traces"),
			Protocol = options.Protocol,
		};

		_exporter = new OtlpTraceExporter(_otlpOptions);
	}

	public override ExportResult Export(in Batch<Activity> batch)
	{
		var resource = this.ParentProvider.GetResource();

		if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpTraceExporter>(_exporter, resource.Attributes.ToDictionary(), out _))
		{
			return ExportResult.Failure;
		}

		var result = _exporter.Export(batch);
		return result;
	}
}
