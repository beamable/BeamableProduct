using beamable.otel.common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace beamable.otel.exporter;

public static class FileExporterMetricsExtensions
{
	private const int DefaultExportIntervalMilliseconds = 300000;
	private const int DefaultExportTimeoutMilliseconds = 60000;

	/// <summary>
	/// Adds <see cref="FileMetricExporter"/> to the <see cref="MeterProviderBuilder"/> using default options.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddFileExporter(this MeterProviderBuilder builder)
		=> AddFileExporter(builder, name: null, configureExporter: null);


	/// <summary>
	/// Adds <see cref="FileMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporter">Callback action for configuring <see cref="FileExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddFileExporter(this MeterProviderBuilder builder, Action<FileExporterOptions> configureExporter)
		=> AddFileExporter(builder, name: null, configureExporter);


	/// <summary>
	/// Adds <see cref="FileMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Optional name which is used when retrieving options.</param>
	/// <param name="configureExporter">Optional callback action for configuring <see cref="FileExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddFileExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<FileExporterOptions>? configureExporter)
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
				sp.GetRequiredService<IOptionsMonitor<FileExporterOptions>>().Get(name),
				sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name));
		});
	}

	/// <summary>
	/// Adds <see cref="FileMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="FileExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddFileExporter(
		this MeterProviderBuilder builder,
		Action<FileExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
		=> AddFileExporter(builder, name: null, configureExporterAndMetricReader);


	/// <summary>
	/// Adds <see cref="FileMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
	/// </summary>
	/// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
	/// <param name="name">Name which is used when retrieving options.</param>
	/// <param name="configureExporterAndMetricReader">Callback action for
	/// configuring <see cref="FileExporterOptions"/> and <see
	/// cref="MetricReaderOptions"/>.</param>
	/// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
	public static MeterProviderBuilder AddFileExporter(
		this MeterProviderBuilder builder,
		string? name,
		Action<FileExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
	{
		if (builder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		return builder.AddReader(sp =>
		{
			var exporterOptions = sp.GetRequiredService<IOptionsMonitor<FileExporterOptions>>().Get(name);
			var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

			configureExporterAndMetricReader?.Invoke(exporterOptions, metricReaderOptions);

			return BuildFileExporterMetricReader(exporterOptions, metricReaderOptions);
		});
	}


	private static PeriodicExportingMetricReader BuildFileExporterMetricReader(
		FileExporterOptions exporterOptions,
		MetricReaderOptions metricReaderOptions)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var metricExporter = new FileMetricExporter(exporterOptions);
#pragma warning restore CA2000 // Dispose objects before losing scope

		return PeriodicExportingMetricReaderHelper.CreatePeriodicExportingMetricReader(
			metricExporter,
			metricReaderOptions,
			DefaultExportIntervalMilliseconds,
			DefaultExportTimeoutMilliseconds);
	}
}
