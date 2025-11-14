using System.Text.Json.Serialization;

namespace cli.Services;

public partial class BeamoLocalSystem
{


	[Serializable]
	public class PortalExtensionPackageInfo
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("version")]
		public string Version { get; set; }

		[JsonPropertyName("beamPortalExtension")]
		public string IsPortalExtension { get; set; }
	}
}
