using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace beamable.otel.exporter.Utils;

internal static class PeriodicExportingMetricReaderHelper
{
	internal const int DefaultExportIntervalMilliseconds = 60000;
	internal const int DefaultExportTimeoutMilliseconds = 30000;

	internal static PeriodicExportingMetricReader CreatePeriodicExportingMetricReader(
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
