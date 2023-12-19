using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;

namespace Beamable.Editor.UI.Model
{
	[Serializable]
	public class BeamoServiceDefinition : IBeamoServiceDefinition
	{
		public bool IsLocal { get; set; }
		public IBeamableBuilder Builder { get; set; }
		public string BeamoId => ServiceInfo.name;
		public ServiceType ServiceType { get; set; }
		public string ImageId { get; set; } = string.Empty;
		public bool ShouldBeEnabledOnRemote { get; set; } = true;
		public bool IsRunningLocally => Builder.IsRunning;
		public BeamoServiceStatus IsRunningOnRemote { get; set; } = BeamoServiceStatus.Unknown;
		public ServiceInfo ServiceInfo { get; set; }
	}
}
