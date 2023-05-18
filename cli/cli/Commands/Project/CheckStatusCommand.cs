using Beamable.Common.BeamCli;
using Beamable.Server;
using Beamable.Server.Common;
using cli.Services;
using cli.Utils;
using NetMQ;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
}

[Serializable]
public class ServiceDiscoveryEvent
{
	public string cid, pid, prefix, service;
	public bool isRunning;
	public bool isContainer;
}

public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, ServiceDiscoveryEvent>
{
	private NetMQBeacon _beacon;

	private Dictionary<string, (long, ServiceDiscoveryEntry)> _nameToEntryWithTimestamp =
		new Dictionary<string, (long, ServiceDiscoveryEntry)>();

	private InterfaceCollection _networkInterfaceCollection;

	public CheckStatusCommand() : base("ps", "List the running status of local services not running in docker")
	{
		_networkInterfaceCollection = new InterfaceCollection();
	}

	public override void Configure()
	{
	}

	public override async Task Handle(CheckStatusCommandArgs args)
	{
		_beacon = new NetMQBeacon();
		_beacon.ConfigureAllInterfaces(Beamable.Common.Constants.Features.Services.DISCOVERY_PORT);
		_beacon.Subscribe("");

		// This doesn't actually block
		await args.BeamoLocalSystem.StartListeningToDocker((beamoId, eventType) =>
		{
			// We skip out on non-Microservice containers.
			var serviceDefinition = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == beamoId);
			if (serviceDefinition == null || serviceDefinition.Protocol != BeamoProtocolType.HttpMicroservice) return;

			var protocol = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceRemoteProtocols[serviceDefinition.BeamoId];
			var isRunning = eventType == "start";
			isRunning &= eventType != "stop";
			this.SendResults(CreateEvent(new ServiceDiscoveryEntry()
			{
				cid = args.AppContext.Cid,
				pid = args.AppContext.Pid,
				prefix = MachineHelper.GetUniqueDeviceId(),
				serviceName = serviceDefinition.BeamoId,
				healthPort = Convert.ToInt32(protocol.HealthCheckPort),
				isContainer = true,
			}, isRunning));
		});

		var toRemove = new HashSet<string>();
		while (true)
		{
			var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var kvp in _nameToEntryWithTimestamp)
			{
				var age = now - kvp.Value.Item1;
				if (age > Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS)
				{
					Log.Information($"{kvp.Key} is off");
					this.SendResults(CreateEvent(kvp.Value.Item2, false));
					_nameToEntryWithTimestamp.Remove(kvp.Key);
					toRemove.Add(kvp.Key);
				}
			}


			foreach (var x in toRemove)
			{
				_nameToEntryWithTimestamp.Remove(x);
			}

			toRemove.Clear();


			if (!TryToListen(out var service))
			{
				continue;
			}

			if (service.cid != args.AppContext.Cid || service.pid != args.AppContext.Pid)
			{
				continue;
			}

			if (!_nameToEntryWithTimestamp.ContainsKey(service.serviceName))
			{
				Log.Information($"{service.serviceName} is available at prefix=[{service.prefix}]");
				this.SendResults(CreateEvent(service, true));
			}

			_nameToEntryWithTimestamp[service.serviceName] = (now, service);


			// cull old entries
			Thread.Sleep(50);
		}

		await args.BeamoLocalSystem.StopListeningToDocker();
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
		};
	}

	private bool TryToListen(out ServiceDiscoveryEntry service)
	{
		service = null;
		var validHostPrefixes = Beamable.Common.Constants.Features.Services.DISCOVERY_IPS.Append("172.");
		if (!_beacon.TryReceive(TimeSpan.FromMilliseconds(50), out var message))
		{
			return false;
		}
		
		var isSelf = _networkInterfaceCollection.Any(item => item.Address.ToString().StartsWith(message.PeerHost));
		if (!isSelf && !validHostPrefixes.Any(prefix => message.PeerHost.StartsWith(prefix)))
		{
			return false;
		}

		var entry = JsonConvert.DeserializeObject<ServiceDiscoveryEntry>(message.String, UnitySerializationSettings.Instance);

		service = entry;
		return true;
	}
}
