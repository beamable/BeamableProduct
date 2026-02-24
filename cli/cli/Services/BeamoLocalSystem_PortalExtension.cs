using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace cli.Services;

public partial class BeamoLocalSystem
{


	public class PortalExtensionPackageInfo
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("beamPortalExtension")]
		public bool IsPortalExtension { get; set; }

		[JsonProperty("portalExtensionType")]
		public string PortalExtensionType { get; set; }

		[JsonProperty("microserviceDependencies")]
		public List<string> MicroserviceDependencies { get; set; }
	}
}
