using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using NetMQ;
using Newtonsoft.Json;
using Serilog;
using System;

namespace microservice.dbmicroservice;

public interface IDiscoveryService
{
	void SetStatus(DiscoveryStatus status);
}

public class NoDiscoveryService : IDiscoveryService
{
	public void SetStatus(DiscoveryStatus _)
	{
		// no-op
	}
}

public static class DiscoveryExtensions
{
	public static IDiscoveryService GetDiscoveryService(this IDependencyProvider provider)
	{
		return provider.GetService<IDiscoveryService>();
	}
}


public class DiscoveryService : IDiscoveryService
{
	private readonly NetMQBeacon _beacon;
	private readonly int _port;
	private readonly ServiceDiscoveryEntry _heartBeatMsg;

	public DiscoveryService(IMicroserviceArgs args, MicroserviceAttribute attribute)
	{
		_beacon = new NetMQBeacon();
		_port = Constants.Features.Services.DISCOVERY_PORT;
		_beacon.Configure(_port);
		_heartBeatMsg = new ServiceDiscoveryEntry
		{
			cid = args.CustomerID,
			pid = args.ProjectName,
			prefix = args.NamePrefix,
			serviceName = attribute.MicroserviceName,
			healthPort = args.HealthPort,
			status = DiscoveryStatus.Starting
		};
	}


	public void SetStatus(DiscoveryStatus status)
	{
		_heartBeatMsg.status = status;
		_beacon.Silence();
		
		var json = HeartBeatMessageJson;
		Log.Verbose("setting discovery json : " + json);
		_beacon.Publish(json, TimeSpan.FromMilliseconds(Constants.Features.Services.DISCOVERY_BROADCAST_PERIOD_MS));
	}

	string HeartBeatMessageJson => JsonConvert.SerializeObject(_heartBeatMsg, UnitySerializationSettings.Instance);
}
