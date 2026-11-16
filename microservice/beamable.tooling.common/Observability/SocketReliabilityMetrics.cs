using System.Diagnostics.Metrics;
using Beamable.Common;

namespace Beamable.Server
{
	/// <summary>
	/// Metrics for outbound requests and websocket connection health. They use the service meter, which
	/// <c>MicroserviceStartupUtil.ConfigureTelemetry</c> already exports.
	/// </summary>
	public static class SocketReliabilityMetrics
	{
		private static readonly Meter Meter = new Meter(Constants.Features.Otel.METER_SERVICE_NAME);

		/// <summary>Outbound requests waiting for a reply.</summary>
		public static readonly UpDownCounter<long> Pending = Meter.CreateUpDownCounter<long>(
			"beam.ws.request.pending", description: "Outbound requests waiting for a reply");

		/// <summary>Outbound requests that passed their reply deadline. Tagged <c>enforced</c> and <c>path</c>.</summary>
		public static readonly Counter<long> DeadlineExceeded = Meter.CreateCounter<long>(
			"beam.ws.request.deadline_exceeded", description: "Outbound requests that passed their reply deadline");

		/// <summary>Outbound requests failed because the connection they were sent on closed first.</summary>
		public static readonly Counter<long> ConnectionLost = Meter.CreateCounter<long>(
			"beam.ws.request.connection_lost", description: "Outbound requests failed because their connection closed");

		/// <summary>Connections a detector judged broken. Tagged <c>reason</c> and <c>enforced</c>.</summary>
		public static readonly Counter<long> UnhealthyConnections = Meter.CreateCounter<long>(
			"beam.ws.connection.unhealthy", description: "Connections judged broken by a health check");

		/// <summary>Connection teardowns. Tagged <c>reason</c>.</summary>
		public static readonly Counter<long> Teardowns = Meter.CreateCounter<long>(
			"beam.ws.connection.teardown", description: "Websocket connection teardowns");
	}
}
