// this file was copied from nuget package Beamable.Server.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.0-PREVIEW.RC6

using Beamable.Common.Api.CloudData;

namespace Beamable.Server.Api.CloudData
{
	/// <summary>
	/// This type defines the %Microservice main entry point for the %A/B %Testing feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/cloud-save-feature-overview">Cloud Saving</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public interface IMicroserviceCloudDataApi : ICloudDataApi
	{
		/* put server-side only methods here */
	}
}
