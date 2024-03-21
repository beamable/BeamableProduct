using System;

namespace Beamable.Server
{
	[Serializable]
	public class ServiceDiscoveryEntry
	{
		public string serviceName;
		public string cid;
		public string pid;
		public string prefix;
		public int healthPort;
		public bool isContainer;
		public string containerId;
		public DiscoveryStatus status;
	}
	
	public enum DiscoveryStatus
	{
		Starting,
		AcceptingTraffic,
		Stopping
	}

	public static class DiscoveryStatusUtil
	{
		public static string GetDisplayString(this DiscoveryStatus status)
		{
			switch (status)
			{
				case DiscoveryStatus.Starting: return "starting";
				case DiscoveryStatus.Stopping: return "stopping";
				case DiscoveryStatus.AcceptingTraffic: return "accepting-traffic";
				default: throw new ArgumentException();
			}
		}
		public static bool TryGetStatusFromString(string statusString, out DiscoveryStatus status)
		{
			switch (statusString)
			{
				case "starting": 
					status = DiscoveryStatus.Starting;
					return true;
				case "stopping": 
					status = DiscoveryStatus.Stopping;
					return true;
				case "accepting-traffic": 
					status = DiscoveryStatus.AcceptingTraffic;
					return true;
				default:
					status = DiscoveryStatus.Starting;
					return false;
			}
		}
	}
}
