using beamable.otel.common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace beamable.microservice.otel.exporter;

public static class MicroserviceOtelMetricsExtensions
{
	private const int DefaultExportIntervalMilliseconds = 300000;
	private const int DefaultExportTimeoutMilliseconds = 60000;

	/// <summary>
	/// Adds <see cref="MicroserviceOtelMetricExporter"/> to the <see cref="MeterProviderBuilder"/> using default options.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddMicroserviceExporter(this MeterProviderBuilder builder)
		=> AddMicroserviceExporter(builder, name: null, configureExporter: null);


	/// <summary>
	/// Adds <see cref="MicroserviceOtelMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporter">Callback action for configuring <see cref="MicroserviceOtelExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddMicroserviceExporter(this MeterProviderBuilder builder, Action<MicroserviceOtelExporterOptions> configureExporter)
		=> AddMicroserviceExporter(builder, name: null, configureExporter);


	/// <summary>
	/// Adds <see cref="MicroserviceOtelMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Optional name which is used when retrieving options.</param>
	/// <param name="configureExporter">Optional callback action for configuring <see cref="MicroserviceOtelExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddMicroserviceExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<MicroserviceOtelExporterOptions>? configureExporter)
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
			return BuildFileExporterMetricReader(
				sp.GetRequiredService<IOptionsMonitor<MicroserviceOtelExporterOptions>>().Get(name),
				sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name));
		});
	}

	/// <summary>
	/// Adds <see cref="MicroserviceOtelMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="MicroserviceOtelExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddMicroserviceExporter(
		this MeterProviderBuilder builder,
		Action<MicroserviceOtelExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
		=> AddMicroserviceExporter(builder, name: null, configureExporterAndMetricReader);


	/// <summary>
	/// Adds <see cref="MicroserviceOtelMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Name which is used when retrieving options.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="MicroserviceOtelExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddMicroserviceExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<MicroserviceOtelExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
	{
		if (builder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		return builder.AddReader(sp =>
		{
			var exporterOptions = sp.GetRequiredService<IOptionsMonitor<MicroserviceOtelExporterOptions>>().Get(name);
			var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

			configureExporterAndMetricReader?.Invoke(exporterOptions, metricReaderOptions);

			return BuildFileExporterMetricReader(exporterOptions, metricReaderOptions);
		});
	}


	private static PeriodicExportingMetricReader BuildFileExporterMetricReader(
		MicroserviceOtelExporterOptions exporterOptions,
		MetricReaderOptions metricReaderOptions)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var metricExporter = new MicroserviceOtelMetricExporter(exporterOptions);
#pragma warning restore CA2000 // Dispose objects before losing scope

		return PeriodicExportingMetricReaderHelper.CreatePeriodicExportingMetricReader(
			metricExporter,
			metricReaderOptions,
			DefaultExportIntervalMilliseconds,
			DefaultExportTimeoutMilliseconds);
	}
}
