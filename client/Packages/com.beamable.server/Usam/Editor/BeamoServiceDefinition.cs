using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;

namespace Usam
{
	[Serializable]
	public class BeamoServiceDefinition : IBeamoServiceDefinition
	{
		public event Action<IBeamoServiceDefinition> Updated;
		public string BeamoId => ServiceInfo.name;
		public ServiceType ServiceType { get; set; } = ServiceType.MicroService;
		public string ImageId { get; set; } = string.Empty;
		public bool ShouldBeEnabledOnRemote { get; set; } = true;
		public ServiceStatus IsRunningLocaly { get; set; } = ServiceStatus.Unknown;
		public ServiceStatus IsRunningOnRemote { get; set; } = ServiceStatus.Unknown;
		public ServiceInfo ServiceInfo  { get; set; }
		public void CallUpdate() => Updated?.Invoke(this);
	}
}
