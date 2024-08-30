using System;

namespace Beamable.Server
{
	[Serializable]
	public class ServiceDiscoveryEntry
	{
		/// <summary>
		/// Value has no semantic meaning when <see cref="isContainer"/> is true.
		/// </summary>
		public int processId;

		public string serviceName;
		public string cid;
		public string pid;
		public string prefix;
		public int healthPort;
		public string serviceType;
		public int dataPort;
		public bool isContainer;
		public string containerId;
		public string executionVersion;
	}
}
