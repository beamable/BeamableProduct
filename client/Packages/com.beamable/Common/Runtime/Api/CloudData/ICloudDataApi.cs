using System.Collections.Generic;

namespace Beamable.Common.Api.CloudData
{
	/// <summary>
	/// This type defines the %Client main entry point for the %A/B %Testing feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/abtesting-feature">A/B Testing</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public interface ICloudDataApi
	{
		Promise<GetCloudDataManifestResponse> GetGameManifest();
		Promise<GetCloudDataManifestResponse> GetPlayerManifest();
	}
}
