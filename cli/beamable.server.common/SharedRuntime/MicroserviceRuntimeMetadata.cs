using System;

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
	}
}
