using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor;
using Beamable.NetMQ;
using Beamable.NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class MicroserviceDiscovery : IBeamableDisposable, ILoadWithContext
	{
		private readonly IBeamableRequester _requester;

		public const float SECONDS_BETWEEN_ZMQ_READS = .1f;
		public const float SECONDS_UNTIL_DATA_RECEIVED = .5f;

		private NetMQBeacon _beacon;
		private ResponseSocket _server;
		private Promise _gotAnyDataPromise;
		private Dictionary<string, DiscoveryData> _nameToLatestEntry =
			new Dictionary<string, DiscoveryData>();

		public MicroserviceDiscovery(IBeamableRequester requester)
		{
			_requester = requester;
			_gotAnyDataPromise = new Promise();
			Start();
		}

		public void Start()
		{
			EditorCoroutineUtility.StartCoroutineOwnerless(Loop());

			_beacon = new NetMQBeacon();
			_beacon.ConfigureAllInterfaces(Beamable.Common.Constants.Features.Services.DISCOVERY_PORT);
			_beacon.Subscribe("");
		}

		IEnumerator Loop()
		{
			var toRemove = new List<string>();
			var timeUntilComplete = SECONDS_UNTIL_DATA_RECEIVED;
			while (true)
			{
				yield return new WaitForSecondsRealtime(SECONDS_BETWEEN_ZMQ_READS);

				if (timeUntilComplete <= 0)
				{
					_gotAnyDataPromise.CompleteSuccess();
				}
				else
				{
					timeUntilComplete -= SECONDS_BETWEEN_ZMQ_READS;
				}

				var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (TryToListen(out var service) && service != null && service.serviceName != null)
				{
					if (service.cid != _requester.Cid || service.pid != _requester.Pid)
					{
						continue; // skip any service for a cid/pid that isn't our current Unity game.
					}

					if (_nameToLatestEntry.TryGetValue(service.serviceName, out var existingData))
					{
						existingData.timestamp = now;
						existingData.entry = service;
					}
					else
					{
						_nameToLatestEntry[service.serviceName] = new DiscoveryData
						{
							entry = service,
							timestamp = now
						};
					}
				}

				// cull out old data...
				toRemove.Clear();
				foreach (var kvp in _nameToLatestEntry)
				{
					var recAt = kvp.Value.timestamp;
					var age = now - recAt;

					if (age > 300) // 300 ms
					{
						toRemove.Add(kvp.Key);
					}
				}

				foreach (var name in toRemove)
				{
					_nameToLatestEntry.Remove(name);
				}
			}
		}

		public async Promise WaitForUpdate()
		{
			await _gotAnyDataPromise;
		}

		public bool TryIsRunning(string serviceName, out ServiceDiscoveryEntry service)
		{
			service = null;

			if (_nameToLatestEntry.TryGetValue(serviceName, out var data))
			{
				service = data.entry;
				return true;
			}

			return false;
		}

		private bool TryToListen(out ServiceDiscoveryEntry service)
		{
			service = null;
			var validHostPrefixes = new string[] { "192.", "0.0.0.0", "127.0.0.1" };
			if (!_beacon.TryReceive(TimeSpan.FromMilliseconds(50), out var message))
			{
				return false;
			}

			if (!validHostPrefixes.Any(prefix => message.PeerHost.StartsWith(prefix)))
			{
				return false;
			}

			var entry = JsonUtility.FromJson<ServiceDiscoveryEntry>(message.String);

			service = entry;
			return true;

		}

		public Promise OnDispose()
		{
			return Promise.Success;
		}

		class DiscoveryData
		{
			public ServiceDiscoveryEntry entry;
			public long timestamp;
		}
	}
}
