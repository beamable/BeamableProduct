using Beamable.AccountManagement;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Stats;
using Beamable.Common.Content;
using Beamable.EasyFeatures.BasicLogin.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLogin
{
	public interface ILoginDeps : IBeamableViewDeps
	{
		/// <summary>
		/// The currently signed in user represented as a <see cref="UserView"/>
		/// </summary>
		UserView CurrentUser { get; }

		/// <summary>
		/// A list of other <see cref="UserView"/>s that exist on the device, and could be set to the current user.
		/// </summary>
		List<UserView> AvailableUsers { get; }

		/// <summary>
		/// When a player wants to switch the current account, they should be given a chance to confirm that decision.
		/// When this field has a <see cref="UserView"/>, it represents the account that can be switched to.
		/// If the field has no value, or is None, then there are is no switch available.
		/// </summary>
		OptionalUserView AvailableSwitch { get; }

		/// <summary>
		/// A <see cref="LoginFlowState"/> describes what the player is currently doing in the login flow.
		/// </summary>
		LoginFlowState State { get; }

		void OfferUserSelection(UserView nextUser);

		void ShowSignInOptions();
	}

	/// <summary>
	/// The enum describes the possible states that the login flow prefab can be in.
	/// </summary>
	public enum LoginFlowState
	{
		/// <summary>
		/// The default, the home screen, shows the current user with all available credential buttons
		/// </summary>
		HOME,

		/// <summary>
		/// When the user is picking what third party option they'd like to use to sign in.
		/// </summary>
		SIGN_IN_OPTIONS,

		/// <summary>
		/// When the user is creating a new anonymous account
		/// </summary>
		NEW_USER,

		THIRD_PARTY_LOGIN,
		ERROR,
		FORGOT_PASSWORD,
		FORGOT_PASSWORD_CONFIRM,

		/// <summary>
		/// When the user is confirming they want to switch accounts
		/// </summary>
		OFFER_SWITCH_USER,
		SET_DETAILS
	}

	[Serializable]
	public struct UserViewThirdPartyCredential
	{
		/// <summary>
		/// The third party auth code
		/// </summary>
		public AuthThirdParty thirdParty;

		/// <summary>
		/// true if the user has the third party enabled on their account
		/// </summary>
		public bool hasCredential;

		/// <summary>
		/// true if game is configured to use this credential type.
		/// </summary>
		public bool isSupported;

		/// <summary>
		/// true if the UI should display that the user has this credential
		/// </summary>
		public bool ShouldShowIcon => isSupported && hasCredential;

		/// <summary>
		/// true if the UI should allow the user to register the credential again
		/// </summary>
		public bool ShouldShowButton => isSupported && !hasCredential;
	}

	[Serializable]
	public struct UserView
	{
		/// <summary>
		/// The gamerTag for the current user. Often refered to as the "dbid"
		/// </summary>
		public long gamerTag;

		/// <summary>
		/// The player's alias that should be used to identify the account to the player
		/// </summary>
		public string alias;

		/// <summary>
		/// A field that can be used to share some context about the current player, like a current level, latest task, or whatever you'd like.
		/// </summary>
		public string subtext;

		/// <summary>
		/// A index into the AvatarConfiguration
		/// </summary>
		public string avatarId;

		/// <summary>
		/// An email address if it has been associated with the user.
		/// </summary>
		public string email;

		/// <summary>
		/// true if the <see cref="email"/> field is not null or empty, suggesting that the user has an email credential
		/// </summary>
		public bool HasEmail => !string.IsNullOrEmpty(email);

		/// <summary>
		/// A set of third party association codes, if they exist.
		/// </summary>
		public string[] thirdPartyAssociations;

		/// <summary>
		/// A set of the <see cref="UserViewThirdPartyCredential"/>s that can be applied to the user.
		/// </summary>
		public UserViewThirdPartyCredential[] thirdParties;

		/// <summary>
		/// A set of device ids associated with the user
		/// </summary>
		public string[] deviceIds;

		/// <summary>
		/// A set of scopes associated with the user
		/// </summary>
		public string[] scopes;

		/// <summary>
		/// The latest accessToken stored for this user.
		/// </summary>
		public string accessToken;

		/// <summary>
		/// The latest refreshToken stored for this user.
		/// </summary>
		public string refreshToken;

		/// <summary>
		/// Get the current <see cref="UserView"/> associated with the given <see cref="BeamContext"/>
		/// </summary>
		/// <param name="statsApi">A <see cref="IStatsApi"/> instance to use to resolve required player stats such as alias.</param>
		/// <param name="config">A <see cref="AccountManagementConfiguration"/> to decide which stats should be used for the player info.</param>
		/// <param name="context">A <see cref="BeamContext"/></param>
		/// <returns>A <see cref="Promise{T}"/> of a <see cref="UserView"/> that is populated with the current user</returns>
		public static async Promise<UserView> GetCurrentUserView(IStatsApi statsApi, AccountManagementConfiguration config, BeamContext context)
		{
			await context.OnReady;
			User user = context.AuthorizedUser;
			var view = new UserView
			{
				email = user.email,
				gamerTag = user.id,
				deviceIds = user.deviceIds.ToArray(),
				thirdPartyAssociations = user.thirdPartyAppAssociations.ToArray(),
				scopes = user.scopes.ToArray(),
				accessToken = context.AccessToken.Token,
				refreshToken = context.AccessToken.RefreshToken,
			};
			view = await ApplyStats(statsApi, config, view, user.id);
			view = await ApplyThirdParties(user, view, config);
			return view;
		}

		/// <summary>
		/// Get a <see cref="UserView"/> for some <see cref="UserBundle"/> representing an available device user.
		/// </summary>
		/// <param name="statsApi">A <see cref="IStatsApi"/> instance to use to resolve required player stats such as alias.</param>
		/// <param name="config">A <see cref="AccountManagementConfiguration"/> to decide which stats should be used for the player info.</param>
		/// <param name="bundle">A <see cref="UserBundle"/> representing an available device user.</param>
		/// <returns>A <see cref="Promise"/> of a <see cref="UserView"/> that is populated with the user data associated from the given <see cref="bundle"/></returns>
		public static async Promise<UserView> GetUserView(IStatsApi statsApi, AccountManagementConfiguration config, UserBundle bundle)
		{
			var view = new UserView
			{
				// pull out the immediately available data.
				email = bundle.User.email,
				gamerTag = bundle.User.id,
				deviceIds = bundle.User.deviceIds.ToArray(),
				thirdPartyAssociations = bundle.User.thirdPartyAppAssociations.ToArray(),
				scopes = bundle.User.scopes.ToArray(),
				accessToken = bundle.Token.access_token,
				refreshToken = bundle.Token.refresh_token
			};

			view = await ApplyStats(statsApi, config, view, bundle.User.id);
			view = await ApplyThirdParties(bundle.User, view, config);
			return view;
		}

		public static async Promise<UserView> ApplyThirdParties(User user, UserView view, AccountManagementConfiguration config)
		{
			var thirdParties = await config.GetAllEnabledThirdPartiesForUser(user);
			view.thirdParties = new UserViewThirdPartyCredential[thirdParties.Count];
			for (var i = 0; i < thirdParties.Count; i++)
			{
				var thirdParty = thirdParties[i];
				view.thirdParties[i] = new UserViewThirdPartyCredential
				{
					isSupported = thirdParty.ThirdPartyEnabled,
					hasCredential = thirdParty.HasAssociation,
					thirdParty = thirdParty.ThirdParty
				};
			}
			return view;
		}

		/// <summary>
		/// Mutate the given <see cref="UserView"/> object with the stat values for the user.
		/// </summary>
		/// <param name="statsApi">A <see cref="IStatsApi"/> instance to use to resolve required player stats such as alias.</param>
		/// <param name="config">A <see cref="AccountManagementConfiguration"/> to decide which stats should be used for the player info.</param>
		/// <param name="view">The <see cref="UserView"/> to modify</param>
		/// <param name="gamerTag">the gamerTag that should be used to source the information that will be written to the <see cref="view"/></param>
		public static async Promise<UserView> ApplyStats(IStatsApi statsApi, AccountManagementConfiguration config, UserView view, long gamerTag)
		{
			var stats = await statsApi.GetStats("client", "public", "player", gamerTag);

			// pull out the alias stat.
			var aliasStatKey = config.DisplayNameStat.StatKey ?? "alias";
			if (!stats.TryGetValue(aliasStatKey, out view.alias))
			{
				view.alias = config.DisplayNameStat.DefaultValue ?? "Anonymous";
			}

			// get the avatar stat.
			var avatarStatKey = config.AvatarStat.StatKey ?? "avatar";
			if (!stats.TryGetValue(avatarStatKey, out view.avatarId))
			{
				view.avatarId = config.AvatarStat.DefaultValue ?? "1";
			}

			// get the subtext
			if (config.SubtextStat)
			{
				var subtextStatKey = config.SubtextStat.StatKey;
				if (!stats.TryGetValue(subtextStatKey, out view.subtext))
				{
					view.subtext = gamerTag.ToString();
				}
			} else {
				view.subtext = gamerTag.ToString();
			}

			return view;
		}
	}

	public static class LoginViewExtensions
	{

		public static UserViewThirdPartyCredential Facebook(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.Facebook);
		public static UserViewThirdPartyCredential Steam(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.Steam);

		public static UserViewThirdPartyCredential Apple(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.Apple);

		public static UserViewThirdPartyCredential Google(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.Google);

		public static UserViewThirdPartyCredential FacebookLimited(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.FacebookLimited);

		public static UserViewThirdPartyCredential GameCenter(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.GameCenter);

		public static UserViewThirdPartyCredential GameCenterLimited(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.GameCenterLimited);

		public static UserViewThirdPartyCredential GooglePlayGameServices(this UserViewThirdPartyCredential[] credentials) =>
			FindThirdParty(credentials, AuthThirdParty.GoogleGamesServices);

		public static UserViewThirdPartyCredential FindThirdParty(this UserViewThirdPartyCredential[] credentials, AuthThirdParty thirdParty)
		{
			for (var i = 0; i < credentials.Length; i++)
			{
				if (credentials[i].thirdParty == thirdParty)
				{
					return credentials[i];
				}
			}

			return new UserViewThirdPartyCredential
			{
				thirdParty = thirdParty, hasCredential = false, isSupported = false
			};
		}
	}

	[Serializable]
	public class OptionalUserView : Optional<UserView>
	{
	}

}
