using System;
using System.Collections.Generic;

namespace Beamable.Server
{
	/// <summary>
	/// Picks which of a service's connections carries a request. Any authenticated connection of the service can send a
	/// platform request, so a request need not wait on a connection that a health check judged broken.
	/// </summary>
	public static class SocketContextSelector
	{
		/// <summary>
		/// The first instance whose connection is healthy, or the first instance when none is.
		/// </summary>
		public static SocketRequesterContext SelectHealthy(IReadOnlyList<IBeamableService> services)
		{
			if (services == null || services.Count == 0) return null;
			try
			{
				for (var i = 0; i < services.Count; i++)
				{
					var context = services[i]?.SocketContext;
					if (context != null && context.IsHealthy) return context;
				}

				return services[0]?.SocketContext;
			}
			catch (ArgumentOutOfRangeException)
			{
				// an instance was being added while this looked; the first one is always there.
				return services[0]?.SocketContext;
			}
		}

		/// <summary>
		/// Uses <paramref name="preferred"/> while it is healthy, and otherwise the first healthy instance.
		/// </summary>
		public static Func<SocketRequesterContext> PreferHealthy(SocketRequesterContext preferred, IReadOnlyList<IBeamableService> services)
		{
			return () =>
			{
				if (preferred == null || preferred.IsHealthy) return preferred;
				return SelectHealthy(services) ?? preferred;
			};
		}
	}
}
