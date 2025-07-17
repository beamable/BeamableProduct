using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace beamable.otel.exporter;

public static class BeamableExporterHelperExtensions
{

	/// <summary>
	/// Adds Beamable exporter to the TracerProvider.
	/// </summary>
	/// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
	/// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
	public static TracerProviderBuilder AddBeamableExporter(this TracerProviderBuilder builder)
		=> AddBeamableExporter(builder, name: null, configure: null);


	/// <summary>
	/// Adds Beamable exporter to the TracerProvider.
	/// </summary>
	/// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
	/// <param name="configure">Callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
	public static TracerProviderBuilder AddBeamableExporter(this TracerProviderBuilder builder, Action<BeamableExporterOptions> configure)
		=> AddBeamableExporter(builder, name: null, configure);


	/// <summary>
	/// Adds Beamable exporter to the TracerProvider.
	/// </summary>
	/// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
	/// <param name="name">Optional name which is used when retrieving options.</param>
	/// <param name="configure">Optional callback action for configuring <see cref="BeamableExporterOptions"/>.</param>
	/// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
	public static TracerProviderBuilder AddBeamableExporter(
		this TracerProviderBuilder builder,
		string? name,
		Action<BeamableExporterOptions>? configure)
	{
		if (builder == null)
		{
			throw new Exception("Builder param cannot be null");
		}

		name ??= Options.DefaultName;

		if (configure != null)
		{
			builder.ConfigureServices(services => services.Configure(name, configure));
		}

		return builder.AddProcessor(sp =>
		{
			var options = sp.GetRequiredService<IOptionsMonitor<BeamableExporterOptions>>().Get(name);

			return new SimpleActivityExportProcessor(new BeamableActivityExporter(options));
		});
	}
}
