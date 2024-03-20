using Beamable.Server;
using Beamable.Server.Common;
using cli.Utils;
using NetMQ;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace cli.Services;

public class DiscoveryService
{
	private readonly BeamoLocalSystem _localSystem;
	private readonly IAppContext _appContext;

	private readonly InterfaceCollection _networkInterfaceCollection;
	private NetMQBeacon _beacon;

	public DiscoveryService(BeamoLocalSystem localSystem, IAppContext appContext)
	{
		_localSystem = localSystem;
		_appContext = appContext;
		_networkInterfaceCollection = new InterfaceCollection();

	}

	public async Task Stop()
	{
		await _localSystem.StopListeningToDocker();
		_beacon.Unsubscribe();
		_beacon.Dispose();
	}

	public async IAsyncEnumerable<ServiceDiscoveryEvent> StartDiscovery(TimeSpan timeout = default, [EnumeratorCancellation] CancellationToken token = default)
	{
		_beacon = new NetMQBeacon();
		_beacon.ConfigureAllInterfaces(Beamable.Common.Constants.Features.Services.DISCOVERY_PORT);
		_beacon.Subscribe("");
		var nameToEntryWithTimestamp = new Dictionary<string, (long, ServiceDiscoveryEntry)>();
		var evtQueue = new ConcurrentQueue<ServiceDiscoveryEvent>();
		var startTime = DateTimeOffset.Now;


		// This doesn't actually block
		await _localSystem.StartListeningToDockerRaw((beamoId, eventType, raw) =>
		{
			// We skip out on non-Microservice containers.
			var serviceDefinition = _localSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == beamoId);
			if (serviceDefinition == null) return;

			var healthPort = 0;

			if (serviceDefinition.Protocol == BeamoProtocolType.HttpMicroservice)
			{
				healthPort = Convert.ToInt32(_localSystem.BeamoManifest.HttpMicroserviceRemoteProtocols[serviceDefinition.BeamoId]
					.HealthCheckPort);
			}
			else
			{
				healthPort =Convert.ToInt32(_localSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols[serviceDefinition.BeamoId]
					.MongoLocalPort);
			}

			var isRunning = eventType == "start";
			isRunning &= eventType != "stop";
			var evt = CreateEvent(new ServiceDiscoveryEntry()
			{
				cid = _appContext.Cid,
				pid = _appContext.Pid,
				prefix = MachineHelper.GetUniqueDeviceId(),
				serviceName = serviceDefinition.BeamoId,
				healthPort = healthPort,
				isContainer = true,
				containerId = raw.ID
			}, isRunning);
			evtQueue.Enqueue(evt);
		});

		var toRemove = new HashSet<string>();
		while (true)
		{

			// return any messages to the caller.
			foreach (var evt in evtQueue)
			{
				yield return evt;
			}
			evtQueue.Clear();

			// check if we have exhausted our ps time.
			var nowTime = DateTimeOffset.Now;
			var duration = nowTime - startTime;
			if (timeout != default && timeout < duration)
			{
				break;
			}

			if (token.IsCancellationRequested)
			{
				break;
			}


			// listen for netMQ messages
			var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var kvp in nameToEntryWithTimestamp)
			{
				var age = now - kvp.Value.Item1;
				if (age > Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS)
				{
					evtQueue.Enqueue(CreateEvent(kvp.Value.Item2, false));
					nameToEntryWithTimestamp.Remove(kvp.Key);
					toRemove.Add(kvp.Key);
				}
			}


			foreach (var x in toRemove)
			{
				nameToEntryWithTimestamp.Remove(x);
			}

			toRemove.Clear();


			if (!TryToListen(out var service))
			{
				continue;
			}

			if (service.cid != _appContext.Cid || service.pid != _appContext.Pid)
			{
				continue;
			}

			if (!nameToEntryWithTimestamp.ContainsKey(service.serviceName))
			{
				evtQueue.Enqueue(CreateEvent(service, true));
			}

			nameToEntryWithTimestamp[service.serviceName] = (now, service);


			// cull old entries
			Thread.Sleep(50);

		}

		await _localSystem.StopListeningToDocker();


	}

	public static ServiceDiscoveryEvent CreateEvent(ServiceDiscoveryEntry entry, bool isRunning)
	{
		return new ServiceDiscoveryEvent
		{
			service = entry.serviceName,
			pid = entry.pid,
			cid = entry.cid,
			prefix = entry.prefix,
			isRunning = isRunning,
			isContainer = entry.isContainer,
			containerId = entry.containerId,
			healthPort = entry.healthPort
		};
	}


	bool TryToListen(out ServiceDiscoveryEntry service)
	{
		service = null;
		if (!_beacon.TryReceive(TimeSpan.FromMilliseconds(50), out var message))
		{
			return false;
		}

		var isSelf = _networkInterfaceCollection.Any(item => item.Address.ToString().StartsWith(message.PeerHost));
		if (!isSelf)
		{
			return false;
		}

		var entry = JsonConvert.DeserializeObject<ServiceDiscoveryEntry>(message.String, UnitySerializationSettings.Instance);

		service = entry;
		return true;
	}


}
