
using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Player Init Federation allows you to run custom server code when a new player token is created.
	/// The player's token request will not complete until the <see cref="CreatePlayer"/> function has finished.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IFederatedPlayerInit<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// The player's new token won't come back until this method completes
		/// </summary>
		/// <param name="account">The account of the player that is initiating.</param>
		/// <param name="properties">The set of properties that were supplied as initProperties on the original request to
		/// create a new token
		/// </param>
		/// <returns></returns>
		Promise<PlayerInitResult> CreatePlayer(FederatedPlayerAccount account, Dictionary<string, string> properties);
	}

	/// <summary>
	/// The account of a player being initialized, as the platform sends it to <see cref="IFederatedPlayerInit{T}.CreatePlayer"/>.
	/// A contract of this federation only: not the account a Beamable API returns.
	/// </summary>
	[Serializable]
	public class FederatedPlayerAccount
	{
		public long id;
		public string email;

		/// <summary>The player's gamer tag in each realm.</summary>
		public List<FederatedPlayerGamerTag> gamerTags;

		public List<FederatedPlayerThirdParty> thirdParties;

		/// <summary>Identities from federated logins; null when there are none.</summary>
		public List<FederatedPlayerExternalIdentity> external;

		public long createdTimeMillis;
		public long updatedTimeMillis;
		public string userName;
		public string country;
		public string language;
		public string roleString;
		public List<string> deviceIds;
		public string realmId;
	}

	/// <summary>A player's gamer tag in one realm.</summary>
	[Serializable]
	public class FederatedPlayerGamerTag
	{
		/// <summary>The realm (pid).</summary>
		public string projectId;

		public long gamerTag;
	}

	/// <summary>A third-party identity attached to the account.</summary>
	[Serializable]
	public class FederatedPlayerThirdParty
	{
		public string name;
		public string appId;
		public string userAppId;
		public string userBusinessId;
		public string email;
	}

	/// <summary>An identity from a federated login.</summary>
	[Serializable]
	public class FederatedPlayerExternalIdentity
	{
		public string providerService;
		public string userId;
		public string providerNamespace;
	}

	[Serializable]
	public class PlayerInitResult
	{
		/// <summary>
		/// an empty string.
		/// </summary>
		public string result;
	}
}
