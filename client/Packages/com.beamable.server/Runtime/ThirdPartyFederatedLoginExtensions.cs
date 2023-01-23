using Beamable.Common;
using Beamable.Common.Api.Auth;

namespace Beamable.Server
{
	public static class ThirdPartyFederatedLoginExtensions
	{
		public static Promise<AttachExternalIdentityResponse> AttachIdentity<T>(this ISupportsFederatedLogin<T> client, string token, ChallengeSolution solution = null)
			where T : IThirdPartyCloudIdentity
		{
			var identity = client.Provider.GetService<T>();
			var api = client.Provider.GetService<IAuthApi>();
			return api.AttachIdentity(token, client.ServiceName, identity.UniqueName, solution);
		}

		public static Promise<DetachExternalIdentityResponse> DetachIdentity<T>(this ISupportsFederatedLogin<T> client, string userId)
			where T : IThirdPartyCloudIdentity
		{
			var identity = client.Provider.GetService<T>();
			var api = client.Provider.GetService<IAuthApi>();
			return api.DetachIdentity(client.ServiceName, userId, identity.UniqueName);
		}

		public static Promise<ExternalAuthenticationResponse> AuthorizeExternalIdentity<T>(this ISupportsFederatedLogin<T> client, string token, ChallengeSolution solution = null)
			where T : IThirdPartyCloudIdentity
		{
			var identity = client.Provider.GetService<T>();
			var api = client.Provider.GetService<IAuthApi>();
			return api.AuthorizeExternalIdentity(token, client.ServiceName,
												 identity.UniqueName,
																 solution);
		}
	}
}
