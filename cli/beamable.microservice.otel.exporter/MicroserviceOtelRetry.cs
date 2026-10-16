namespace beamable.microservice.otel.exporter;

/// <summary>
/// Process-wide switch for the microservice exporters' retry queues. Once retry is disabled for an
/// endpoint, exporters sending there drop failed exports instead of queueing them, so telemetry that can
/// never be delivered doesn't build up in memory. Exporters sending anywhere else keep retrying.
/// </summary>
public static class MicroserviceOtelRetry
{
	private static volatile string? _disabledEndpoint;

	public static bool IsDisabledFor(string endpoint)
	{
		var disabled = _disabledEndpoint;
		return disabled != null && disabled == endpoint;
	}

	/// <summary>
	/// Stop queueing failed exports to <paramref name="endpoint"/>, for example a collector that is gone for
	/// good. Batches already queued for it are dropped on each exporter's next export.
	/// </summary>
	public static void DisableFor(string endpoint) => _disabledEndpoint = endpoint;

	internal static void ResetForTests() => _disabledEndpoint = null;
}
