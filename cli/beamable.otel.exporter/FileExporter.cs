using OpenTelemetry;

namespace beamable.otel.exporter;

public abstract class FileExporter<T> : BaseExporter<T>
	where T : class
{
	private readonly FileExporterOptions options;

	protected FileExporter(FileExporterOptions options)
	{
		this.options = options ?? new FileExporterOptions();
	}
}
