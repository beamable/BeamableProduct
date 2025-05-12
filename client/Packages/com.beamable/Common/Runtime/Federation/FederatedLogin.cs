// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

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
	    /// <summary>
        /// Authenticates a user using the given token, challenge, and solution.
        /// This interface covers both the login and signup flows.
        /// </summary>
        /// <param name="token">The token to authenticate with.</param>
        /// <param name="challenge">The challenge to authenticate with.</param>
        /// <param name="solution">The solution to authenticate with.</param>
        /// <returns>A promise that resolves to a federated authentication response.</returns>
        /// <remarks>
        /// When it implements the 2-step authentication flow it is called two times, first with the token having any value only.
        /// The second time it is called, the token will be the token returned from the first call,
        /// the challenge will be the challenge returned from the first call, and the solution will be the solution provided by the user.
        /// </remarks>
        /// <example>
        /// Simplified 1-step authentication flow implementation example:
        /// <code>
        ///    public async Promise&lt;FederatedAuthenticationResponse&gt; Authenticate(string token, string challenge,
        ///        string solution)
        ///    {
        ///        var externalId = await CallExternalAuthProvider(token);
        ///        if (!string.IsNullOrWhiteSpace(externalId))
        ///        {
        ///            throw new MicroserviceException(500, "Authentication failed", "Authentication failed.");
        ///        }
        ///         return new FederatedAuthenticationResponse()
        ///        {
        ///            user_id = externalId
        ///        };
        ///    }
        /// </code>
        /// Simplified 2-step authentication flow implementation example:
        /// <code>
        ///    public Promise&lt;FederatedAuthenticationResponse&gt; Authenticate(string token, string challenge,
        ///        string solution)
        ///    {
        ///        if (string.IsNullOrWhiteSpace(solution))
        ///        {
        ///               var (challengeText, challengeTime) = await StartThirdPartyVerification(token);
        ///            return new FederatedAuthenticationResponse()
        ///            {
        ///                challenge = challengeText,
        ///                challenge_ttl = challengeTime
        ///            };
        ///        }
        ///        var externalId = await ValidateThirdParty(token, solution);
        ///        if (string.IsNullOrWhiteSpace(externalId))
        ///        {
        ///          throw new MicroserviceException(500, "Authentication failed", "Authentication failed.");
        ///        }
        ///        return new FederatedAuthenticationResponse()
        ///        {
        ///          user_id = externalId
        ///        };
        /// }
        /// </code>
        /// </example>
		Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution);
	}

	[Serializable]
	public class FederatedAuthenticationResponse : ExternalAuthenticationResponse
	{
		// exists for typing purposes.

		/// <summary>
		/// Creates a successful response with the given user ID.
		/// </summary>
		/// <param name="userId">The user ID to associate with the response. This should be the external provider id, not the Beamable one.</param>
		/// <returns>A successful response with the given user ID.</returns>
		public static FederatedAuthenticationResponse Success(string userId)
		{
			return new FederatedAuthenticationResponse()
			{
				user_id = userId,
			};
		}

        /// <summary>
        /// Creates a pending validation response with the given challenge and challenge TTL.
        /// </summary>
        /// <param name="challenge">The challenge to associate with the response.</param>
        /// <param name="challenge_ttl">The challenge TTL to associate with the response.</param>
        /// <returns>A pending validation response with the given challenge and challenge TTL.</returns>
		public static FederatedAuthenticationResponse PendingValidation(string challenge, int challenge_ttl)
		{
			return new FederatedAuthenticationResponse()
			{
				challenge = challenge,
				challenge_ttl = challenge_ttl,
			};
		}
	}
}
