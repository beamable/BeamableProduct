using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;

namespace Beamable.Editor.UI.Model
{
	[Serializable]
	public class BeamoServiceDefinition : IBeamoServiceDefinition
	{
		public IBeamableBuilder Builder { get; set; }
		public event Action<IBeamoServiceDefinition> Updated;
		public string BeamoId => ServiceInfo.name;
		public ServiceType ServiceType { get; set; } = ServiceType.MicroService;
		public string ImageId { get; set; } = string.Empty;
		public bool ShouldBeEnabledOnRemote { get; set; } = true;
		public BeamoServiceStatus IsRunningLocally { get; set; } = BeamoServiceStatus.Unknown;
		public BeamoServiceStatus IsRunningOnRemote { get; set; } = BeamoServiceStatus.Unknown;
		public ServiceInfo ServiceInfo { get; set; }
		public void CallUpdate() => Updated?.Invoke(this);
	}
}
