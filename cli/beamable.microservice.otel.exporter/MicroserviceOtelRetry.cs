namespace beamable.microservice.otel.exporter;

/// <summary>
/// Process-wide switch for the microservice exporters' retry queues. Once disabled, failed exports are
/// dropped instead of queued, so telemetry that can never be delivered doesn't build up in memory.
/// </summary>
public static class MicroserviceOtelRetry
{
	private static volatile bool _disabled;

	public static bool Disabled => _disabled;

	/// <summary>
	/// Stop queueing failed exports. Batches already queued are dropped on each exporter's next export.
	/// </summary>
	public static void Disable() => _disabled = true;

	internal static void ResetForTests() => _disabled = false;
}
