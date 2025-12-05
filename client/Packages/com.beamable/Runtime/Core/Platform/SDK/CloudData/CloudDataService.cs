using Beamable.Common.Api;
using Beamable.Common.Api.CloudData;

namespace Beamable.Api.CloudData
{
	/// <summary>
	/// This type defines the %Client main entry point for the %A/B %Testing feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-4.0/unity/user-reference/beamable-services/bi/ab-testing-overview/">A/B Testing</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class CloudDataService : CloudDataApi
	{
		public CloudDataService(IUserContext ctx, IBeamableRequester requester, IHttpRequester httpRequester) : base(ctx, requester, httpRequester)
		{
		}
	}
}
