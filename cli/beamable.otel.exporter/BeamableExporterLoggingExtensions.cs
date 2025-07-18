using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace beamable.otel.exporter;

public static class BeamableExporterLoggingExtensions
{
	/// <summary>
	/// Adds Beamable exporter with OpenTelemetryLoggerOptions.
	/// </summary>
	/// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
	/// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
	public static OpenTelemetryLoggerOptions AddBeamableExporter(this OpenTelemetryLoggerOptions loggerOptions)
		=> AddBeamableExporter(loggerOptions, configure: null);

	/// <summary>
	/// Adds Beamable exporter with OpenTelemetryLoggerOptions.
	/// </summary>
	/// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
	/// <param name="configure">Optional callback action for configuring <see cref="ConsoleExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
	public static OpenTelemetryLoggerOptions AddBeamableExporter(this OpenTelemetryLoggerOptions loggerOptions, Action<BeamableExporterOptions>? configure)
	{
		if (loggerOptions == null)
		{
			throw new Exception("LoggerOptions param cannot be null");
		}

		var options = new BeamableExporterOptions();
		configure?.Invoke(options);
#pragma warning disable CA2000 // Dispose objects before losing scope
		return loggerOptions.AddProcessor(new BatchLogRecordExportProcessor(new BeamableLogRecordExporter(options)));
#pragma warning restore CA2000 // Dispose objects before losing scope
	}


	/// <summary>
	/// Adds Beamable exporter with LoggerProviderBuilder.
	/// </summary>
	/// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
	/// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
	public static LoggerProviderBuilder AddBeamableExporter(
		this LoggerProviderBuilder loggerProviderBuilder)
		=> AddBeamableExporter(loggerProviderBuilder, name: null, configure: null);

	/// <summary>
	/// Adds Console exporter with LoggerProviderBuilder.
	/// </summary>
	/// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
	/// <param name="configure">Callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
	public static LoggerProviderBuilder AddBeamableExporter(
		this LoggerProviderBuilder loggerProviderBuilder,
		Action<BeamableExporterOptions> configure)
		=> AddBeamableExporter(loggerProviderBuilder, name: null, configure);


	/// <summary>
	/// Adds Console exporter with LoggerProviderBuilder.
	/// </summary>
	/// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
	/// <param name="name">Optional name which is used when retrieving options.</param>
	/// <param name="configure">Optional callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
	public static LoggerProviderBuilder AddBeamableExporter(
		this LoggerProviderBuilder loggerProviderBuilder,
		string? name,
		Action<BeamableExporterOptions>? configure)
	{
		if (loggerProviderBuilder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		if (configure != null)
		{
			loggerProviderBuilder.ConfigureServices(services => services.Configure(name, configure));
		}

		return loggerProviderBuilder.AddProcessor(sp =>
		{
			var options = sp.GetRequiredService<IOptionsMonitor<BeamableExporterOptions>>().Get(name);

			return new BatchLogRecordExportProcessor(new BeamableLogRecordExporter(options));
		});
	}
}
