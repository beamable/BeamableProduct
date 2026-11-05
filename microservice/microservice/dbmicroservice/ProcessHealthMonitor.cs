using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ZLogger;

namespace Beamable.Server
{
	/// <summary>
	/// Checks the service's connections once a second on its own thread, so the checks keep running even when the
	/// thread pool is starved.
	/// </summary>
	public class ProcessHealthMonitor
	{
		private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

		private readonly IReadOnlyList<IBeamableService> _services;
		private volatile bool _stopped;

		public ProcessHealthMonitor(IReadOnlyList<IBeamableService> services)
		{
			_services = services;
		}

		public void Start()
		{
			new Thread(Run) { IsBackground = true, Name = "beam-health-monitor" }.Start();
		}

		public void Stop() => _stopped = true;

		private void Run()
		{
			while (!_stopped)
			{
				try
				{
					Check(Stopwatch.GetTimestamp());
				}
				catch (Exception ex)
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogError($"The health monitor failed a check. {ex.GetType().Name}: {ex.Message}");
				}

				Thread.Sleep(Interval);
			}
		}

		/// <summary>One round of checks at <paramref name="now"/>, a <see cref="Stopwatch.GetTimestamp"/>.</summary>
		internal void Check(long now)
		{
			for (var i = 0; i < _services.Count; i++)
			{
				_services[i]?.SocketContext?.CheckForStall(now);
			}
		}
	}
}
