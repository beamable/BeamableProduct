using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace beamable.otel.common;

public static class PeriodicExportingMetricReaderHelper
{
	public const int DefaultExportIntervalMilliseconds = 300000;
	public const int DefaultExportTimeoutMilliseconds = 60000;

	public static PeriodicExportingMetricReader CreatePeriodicExportingMetricReader(
		BaseExporter<Metric> exporter,
		MetricReaderOptions options,
		int defaultExportIntervalMilliseconds = DefaultExportIntervalMilliseconds,
		int defaultExportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds)
	{
		var exportInterval =
			options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds ?? defaultExportIntervalMilliseconds;

		var exportTimeout =
			options.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds ?? defaultExportTimeoutMilliseconds;

		var metricReader = new PeriodicExportingMetricReader(exporter, exportInterval, exportTimeout)
		{
			TemporalityPreference = options.TemporalityPreference,
		};

		return metricReader;
	}
}
