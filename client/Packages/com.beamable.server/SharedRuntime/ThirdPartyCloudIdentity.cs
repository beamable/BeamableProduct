using Beamable.Common;
using Beamable.Common.Api.Auth;
using BeamableReflection;

namespace Beamable.Server
{
	[Preserve]
	public interface IThirdPartyCloudIdentity
	{
		string UniqueName { get; }
	}

	public class SolanaCloudIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "Solana";
	}

	public class PolygonCloudIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "Polygon";
	}

	public interface IFederatedLogin<in T> where T : IThirdPartyCloudIdentity
	{
		ExternalAuthenticationResponse Authenticate(string token, string challenge, string solution);
	}

	public interface IHaveServiceName
	{
		string ServiceName { get; }
	}

	public interface ISupportsFederatedLogin<T> : IHaveServiceName
		where T : IThirdPartyCloudIdentity
	{
		
	}

}
