using Beamable.Common.Api.Auth;
using System;

namespace Beamable.Common
{
	/// <summary>
	/// Login federation allows you to create federate the login/signup flows of Beamable to one or more third-parties.
	/// It also allows you to run arbitrary code every time a user logs in.
	/// </summary>
	public interface IFederatedLogin<in T> : IFederation where T : IFederationId, new()
	{
		Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution);
	}

	[Serializable]
	public class FederatedAuthenticationResponse : ExternalAuthenticationResponse
	{
		// exists for typing purposes.
	}
}