
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamServiceReference
	{
		public string serviceName;
		public string checksum;
		public bool enabled;
		public string imageId;
		public string templateId;
		public string comments;
		public System.Collections.Generic.List<BeamServiceDependency> dependencies;
		public long containerHealthCheckPort;
		public System.Collections.Generic.List<Beamable.Api.Autogenerated.Models.ServiceComponent> components;
	}
}