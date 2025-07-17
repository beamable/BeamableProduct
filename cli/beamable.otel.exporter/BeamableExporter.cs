using OpenTelemetry;

namespace beamable.otel.exporter;

public abstract class BeamableExporter<T> : BaseExporter<T>
	where T : class
{
	private readonly BeamableExporterOptions options;

	protected BeamableExporter(BeamableExporterOptions options)
	{
		this.options = options ?? new BeamableExporterOptions();
	}
}
