using Beamable.Common;
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
	[InitializeOnLoad]
	public class UsageExample
	{
		static UsageExample()
		{
			Demo();
		}
		
		static async void Demo()
		{
			var ctx = BeamEditorContext.Default;
			await ctx.InitializePromise;
	
			var discovery = ctx.ServiceScope.GetService<MicroserviceDiscovery>();
			discovery.Start();
		}
		
		[MenuItem("ZMQ/check")]
	
		public static async void Check()
		{
			var ctx = BeamEditorContext.Default;
			await ctx.InitializePromise;
	
			var discovery = ctx.ServiceScope.GetService<MicroserviceDiscovery>();
			// var name = "MMV3Publish2";
			var name = "standalone-microservice";
			if (discovery.TryIsRunning(name, out var data))
			{
				Debug.Log("Running! " + data.prefix);
			}
			else
			{
				Debug.Log("not running");
			}
		}
	}
	
	public class MicroserviceDiscovery : IBeamableDisposable
	{
		private readonly BeamEditorContext _ctx;
		private NetMQBeacon _beacon;
		private ResponseSocket _server;

		private Dictionary<string, DiscoveryData> _nameToLatestEntry =
			new Dictionary<string, DiscoveryData>();

		public MicroserviceDiscovery( BeamEditorContext ctx)
		{
			_ctx = ctx;
		}

		void Check()
		{
			
		}
		
		public void Start()
		{
			EditorCoroutineUtility.StartCoroutineOwnerless(Loop());
			
			_beacon = new NetMQBeacon();
			_beacon.ConfigureAllInterfaces(Beamable.Common.Constants.Features.Services.DISCOVERY_PORT);
			_beacon.Publish("unity"); // TODO: remove
			_beacon.Subscribe("");
			
			Debug.Log($"Beamable-Beacon waiting for connections.... [{_beacon.BoundTo}] [{_beacon.HostName}] [{_beacon.IsDisposed}]");
		}

		IEnumerator Loop()
		{
			var toRemove = new List<string>();
			while (true)
			{
				yield return new WaitForSecondsRealtime(.1f);
				var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (TryToListen(out var service) && service != null && service.serviceName != null)
				{
					if (service.cid != _ctx.Requester.Cid || service.pid != _ctx.Requester.Pid)
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

		public bool TryIsRunning(string serviceName, out ServiceDiscoveryEntry service)
		{
			service = null;

			foreach (var kvp in _nameToLatestEntry)
			{
				Debug.Log($"Service: {kvp.Key} - {kvp.Value.timestamp} - {kvp.Value.entry.prefix}");
			}
			
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
			var validHostPrefixes = new string[] {"192.", "0.0.0.0", "127.0.0.1"};
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
			// _beacon.Unsubscribe();
			// _beacon.Silence();
			return Promise.Success;
		}

		class DiscoveryData
		{
			public ServiceDiscoveryEntry entry;
			public long timestamp;
		}
	}
}
