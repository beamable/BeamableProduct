using Beamable.Coroutines;
using System.Collections;
using UnityEngine;

namespace Beamable.Api.Sessions
{
	public class Heartbeat
	{
		private const int HeartbeatInterval = 30;

		private readonly PlatformService _platform;
		private readonly CoroutineService _coroutineService;
		private IEnumerator _heartbeatRoutine;

		public Heartbeat(PlatformService platform, CoroutineService coroutineService)
		{
			_platform = platform;
			_coroutineService = coroutineService;
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
				if (_platform.ConnectivityService.HasConnectivity)
				{
					_platform.Session.SendHeartbeat();
				}

				yield return wait;
			}
		}
	}
}
