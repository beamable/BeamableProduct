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
	}
}
