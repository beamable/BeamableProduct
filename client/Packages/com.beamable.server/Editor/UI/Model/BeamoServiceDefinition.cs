using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.UI.Model
{
	[Serializable]
	public class BeamoServiceDefinition : IBeamoServiceDefinition
	{
		public bool HasLocalSource { get; set; }
		public IBeamableBuilder Builder { get; set; }
		public string BeamoId => ServiceInfo.name;
		public ServiceType ServiceType { get; set; }
		public string ImageId { get; set; } = string.Empty;
		public bool ShouldBeEnabledOnRemote { get; set; } = true;
		public bool IsRunningLocally => Builder.IsRunning;
		public BeamoServiceStatus IsRunningOnRemote { get; set; } = BeamoServiceStatus.Unknown;
		public ServiceInfo ServiceInfo { get; set; }
		public bool ExistLocally => !string.IsNullOrWhiteSpace(ServiceInfo?.projectPath);
		public List<string> Dependencies { get; set; }
	}
}
