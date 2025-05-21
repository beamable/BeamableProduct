// this file was copied from nuget package Beamable.Server.Common@4.2.0
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0

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
		public long startedByAccountId;

	}

	public struct ServiceDiscoveryEntryIdentity
	{
		public bool Equals(ServiceDiscoveryEntryIdentity other)
		{
			return service == other.service && routingKey == other.routingKey && startedById == other.startedById;
		}

		public override bool Equals(object obj)
		{
			return obj is ServiceDiscoveryEntryIdentity other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (service != null ? service.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (routingKey != null ? routingKey.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ startedById.GetHashCode();
				return hashCode;
			}
		}

		public string service;
		public string routingKey;
		public long startedById;

		public ServiceDiscoveryEntryIdentity(string service, string routingKey, long startedById)
		{
			this.service = service;
			this.startedById = startedById;
			this.routingKey = routingKey;
		}
	}
}
