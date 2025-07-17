using beamable.otel.exporter.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace beamable.otel.exporter;

public static class BeamableExporterMetricsExtensions
{
	private const int DefaultExportIntervalMilliseconds = 10000;
	private const int DefaultExportTimeoutMilliseconds = Timeout.Infinite;

	/// <summary>
	/// Adds <see cref="BeamableMetricExporter"/> to the <see cref="MeterProviderBuilder"/> using default options.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddBeamableExporter(this MeterProviderBuilder builder)
		=> AddBeamableExporter(builder, name: null, configureExporter: null);


	/// <summary>
	/// Adds <see cref="BeamableMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporter">Callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddBeamableExporter(this MeterProviderBuilder builder, Action<BeamableExporterOptions> configureExporter)
		=> AddBeamableExporter(builder, name: null, configureExporter);


	/// <summary>
	/// Adds <see cref="BeamableMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Optional name which is used when retrieving options.</param>
	/// <param name="configureExporter">Optional callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddBeamableExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<BeamableExporterOptions>? configureExporter)
	{
		if (builder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		if (configureExporter != null)
		{
			builder.ConfigureServices(services => services.Configure(name, configureExporter));
		}

		return builder.AddReader(sp =>
		{
			return BuildBeamableExporterMetricReader(
				sp.GetRequiredService<IOptionsMonitor<BeamableExporterOptions>>().Get(name),
				sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name));
		});
	}

	/// <summary>
	/// Adds <see cref="BeamableMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="BeamableExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddBeamableExporter(
		this MeterProviderBuilder builder,
		Action<BeamableExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
		=> AddBeamableExporter(builder, name: null, configureExporterAndMetricReader);


	/// <summary>
	/// Adds <see cref="BeamableMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Name which is used when retrieving options.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="BeamableExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddBeamableExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<BeamableExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
	{
		if (builder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		return builder.AddReader(sp =>
		{
			var exporterOptions = sp.GetRequiredService<IOptionsMonitor<BeamableExporterOptions>>().Get(name);
			var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

			configureExporterAndMetricReader?.Invoke(exporterOptions, metricReaderOptions);

			return BuildBeamableExporterMetricReader(exporterOptions, metricReaderOptions);
		});
	}


	private static PeriodicExportingMetricReader BuildBeamableExporterMetricReader(
		BeamableExporterOptions exporterOptions,
		MetricReaderOptions metricReaderOptions)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var metricExporter = new BeamableMetricExporter(exporterOptions);
#pragma warning restore CA2000 // Dispose objects before losing scope

		return PeriodicExportingMetricReaderHelper.CreatePeriodicExportingMetricReader(
			metricExporter,
			metricReaderOptions,
			DefaultExportIntervalMilliseconds,
			DefaultExportTimeoutMilliseconds);
	}
}
