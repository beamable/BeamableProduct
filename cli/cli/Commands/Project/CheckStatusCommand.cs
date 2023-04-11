using Beamable.Common.BeamCli;
using Beamable.Server;
using Beamable.Server.Common;
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
	
}



public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, ServiceDiscoveryEvent>
{
	private NetMQBeacon _beacon;

	private Dictionary<string, (long, ServiceDiscoveryEntry)> _nameToEntryWithTimestamp =
		new Dictionary<string, (long, ServiceDiscoveryEntry)>();

	public CheckStatusCommand() : base("ps", "List the running status of local services not running in docker")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(CheckStatusCommandArgs args)
	{
		_beacon = new NetMQBeacon();
		_beacon.ConfigureAllInterfaces(Beamable.Common.Constants.Features.Services.DISCOVERY_PORT);
		_beacon.Subscribe("");
		var toRemove = new HashSet<string>();
		while (true)
		{
			var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var kvp in _nameToEntryWithTimestamp)
			{
				var age = now - kvp.Value.Item1;
				if (age > 350)
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
	}

	public static ServiceDiscoveryEvent CreateEvent(ServiceDiscoveryEntry entry, bool isRunning)
	{
		return new ServiceDiscoveryEvent
		{
			service = entry.serviceName,
			pid = entry.pid,
			cid = entry.cid,
			prefix = entry.prefix,
			isRunning = isRunning
		};
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

		var entry = JsonConvert.DeserializeObject<ServiceDiscoveryEntry>(message.String, UnitySerializationSettings.Instance);
		
		service = entry;
		return true;

	}
	
}
