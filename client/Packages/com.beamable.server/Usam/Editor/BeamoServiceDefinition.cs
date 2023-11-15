using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;

namespace Usam
{
	public class BeamoServiceDefinition : IBeamoServiceDefinition
	{
		public string BeamoId => ServiceInfo.name;
		public ServiceType ServiceType { get; set; } = ServiceType.MicroService;
		public string ImageId { get; set; } = string.Empty;
		public bool ShouldBeEnabledOnRemote { get; set; } = true;
		public ServiceStatus IsRunningLocaly { get; set; } = ServiceStatus.Unknown;
		public ServiceStatus IsRunningOnRemote { get; set; } = ServiceStatus.Unknown;
		public ServiceInfo ServiceInfo  { get; set; }
		
		public BeamoServiceDefinition(){}

		public BeamoServiceDefinition(ServiceInfo info)
		{
			ServiceInfo = info;
		}

		public void Stop()
		{
			throw new System.NotImplementedException();
		}
	}
}
