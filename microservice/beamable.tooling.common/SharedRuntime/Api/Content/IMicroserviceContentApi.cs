using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Common.Content;

namespace Beamable.Server.Api.Content
{
	/// <summary>
	/// This type defines the %Microservice main entry point for the %Content feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview/-overview">Content</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public interface IMicroserviceContentApi : IContentApi
	{
		Promise<TContent> Resolve<TContent>(IContentRef<TContent> reference) where TContent : IContentObject, new();
	}
}
