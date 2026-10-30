using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Beamable.Common;
using Beamable.Server;

namespace Beamable.Server
{
	/// <summary>
	/// Metrics for outbound requests and websocket connection health. They use the service meter, which
	/// <c>MicroserviceStartupUtil.ConfigureTelemetry</c> already exports.
	/// </summary>
	public static class SocketReliabilityMetrics
	{
		private static readonly Meter Meter = new Meter(Constants.Features.Otel.METER_SERVICE_NAME);

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

		private static readonly object ContextsLock = new object();
		private static readonly List<WeakReference<SocketRequesterContext>> Contexts = new List<WeakReference<SocketRequesterContext>>();

		static SocketReliabilityMetrics()
		{
			Meter.CreateObservableGauge("beam.ws.request.pending", ObservePending,
				description: "Outbound requests waiting for a reply, per connection");
			Meter.CreateObservableGauge("beam.ws.request.oldest_pending_ms", ObserveOldest, unit: "ms",
				description: "Age of the oldest outbound request waiting for a reply, per connection");
		}

		internal static void Track(SocketRequesterContext context)
		{
			lock (ContextsLock)
			{
				Contexts.Add(new WeakReference<SocketRequesterContext>(context));
			}
		}

		private static IEnumerable<Measurement<long>> ObservePending() =>
			Observe(c => c.PendingRequestCount);

		private static IEnumerable<Measurement<long>> ObserveOldest() =>
			Observe(c => (long)c.OldestPendingRequestAge.TotalMilliseconds);

		private static IEnumerable<Measurement<long>> Observe(Func<SocketRequesterContext, long> read)
		{
			var measurements = new List<Measurement<long>>();
			lock (ContextsLock)
			{
				Contexts.RemoveAll(r => !r.TryGetTarget(out _));
				foreach (var reference in Contexts)
				{
					if (!reference.TryGetTarget(out var context)) continue;
					measurements.Add(new Measurement<long>(read(context),
						new KeyValuePair<string, object>("connection", context.Name ?? "")));
				}
			}

			return measurements;
		}
	}
}
