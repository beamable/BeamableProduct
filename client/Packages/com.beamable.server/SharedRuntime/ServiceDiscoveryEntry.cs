// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407221259
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407221259

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
		public string serviceType;
		public int dataPort;
		public bool isContainer;
		public string containerId;
	}
}
