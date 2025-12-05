using OpenTelemetry;

namespace beamable.microservice.otel.exporter;

public abstract class MicroserviceOtelExporter<T> : BaseExporter<T>
	where T : class
{
	private readonly MicroserviceOtelExporterOptions options;

	protected MicroserviceOtelExporter(MicroserviceOtelExporterOptions options)
	{
		this.options = options ?? new MicroserviceOtelExporterOptions();
	}
}
