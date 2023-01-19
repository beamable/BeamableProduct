using BeamableReflection;

namespace Beamable.Common.Api.Auth
{
	[Preserve]
	public interface IThirdPartyCloudIdentity
	{
		string UniqueName { get; }
	}
}
