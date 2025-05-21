// this file was copied from nuget package Beamable.Server.Common@4.2.0
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0

using System;
using System.Collections.Generic;

namespace beamable.server
{
	[Serializable]
	public class MicroserviceRuntimeMetadata
	{
		public string serviceName;
		public string sdkVersion;
		public string sdkBaseBuildVersion;
		public string sdkExecutionVersion;
		public bool useLegacySerialization;
		public bool disableAllBeamableEvents;
		public bool enableEagerContentLoading;
		public string instanceId;
		public string routingKey;

		public List<FederationComponentMetadata> federatedComponents = new List<FederationComponentMetadata>();
	}

	public class FederationComponentMetadata
	{
		public string federationNamespace;
		public string federationType;
	}
}
