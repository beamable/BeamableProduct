// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407161549
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407161549

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
