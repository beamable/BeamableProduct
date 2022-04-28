using Beamable.Api.Connectivity;
using Beamable.Coroutines;
using System.Collections;
using UnityEngine;

namespace Beamable.Api.Sessions
{
	public interface IHeartbeatService
	{
		/// <summary>
		/// Start sending heartbeats with the <see cref="SessionService.SendHeartbeat"/> method.
		/// </summary>
		void Start();

		/// <summary>
		/// Reset the interval between heartbeats to the default value 30 seconds.
		/// </summary>
		void ResetInterval();

		/// <summary>
		/// Change how often a heartbeat is sent to the session.
		/// <see cref="ResetInterval"/> will revert the interval back to the default setting.
		/// </summary>
		/// <param name="seconds">The number of seconds between heartbeats.</param>
		void UpdateInterval(int seconds);
	}
	public class Heartbeat : IHeartbeatService
	{
		private const int HeartbeatInterval = 30;

		private readonly ISessionService _sessionService;
		private readonly CoroutineService _coroutineService;
		private readonly IConnectivityService _connectivityService;
		private IEnumerator _heartbeatRoutine;

		public Heartbeat(ISessionService sessionService, CoroutineService coroutineService, IConnectivityService connectivityService)
		{
			_sessionService = sessionService;
			_coroutineService = coroutineService;
			_connectivityService = connectivityService;
			_heartbeatRoutine = SendHeartbeat(HeartbeatInterval);
		}

		public void Start()
		{
			_coroutineService.StartCoroutine(_heartbeatRoutine);
		}

		public void ResetInterval()
		{
			UpdateInterval(HeartbeatInterval);
		}

		public void UpdateInterval(int seconds)
		{
			_coroutineService.StopCoroutine(_heartbeatRoutine);
			_heartbeatRoutine = SendHeartbeat(seconds);
			_coroutineService.StartCoroutine(_heartbeatRoutine);
		}

		/// <summary>
		/// Coroutine: send heartbeat requests to Platform.
		/// </summary>
		private IEnumerator SendHeartbeat(int intervalSeconds)
		{
			var wait = new WaitForSeconds(intervalSeconds);
			while (true)
			{
				if (_connectivityService.HasConnectivity)
				{
					_sessionService.SendHeartbeat();
				}
				yield return wait;
			}
		}
	}
}
