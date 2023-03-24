using Beamable.Common;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
	public class MyIdent : IThirdPartyCloudIdentity
	{
		public string UniqueName => "chrisland";
	}
	
	public class TunaSala : IThirdPartyCloudIdentity
	{
		public string UniqueName => "tunasalad";
	}
}
