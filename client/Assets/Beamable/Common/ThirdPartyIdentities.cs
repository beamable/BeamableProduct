using Beamable.Common;
using Beamable.Common.Api.Auth;

// TODO: this one should not be a part of final push/merge, this is the client/customer side implementation
namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
	public class ThirdPartyIdentities
	{
		public class SolanaCloudIdentity : IThirdPartyCloudIdentity
		{
			public string UniqueName => "Solana";
		}

		public class PolygonCloudIdentity : IThirdPartyCloudIdentity
		{
			public string UniqueName => "Polygon";
		}
	}
}
