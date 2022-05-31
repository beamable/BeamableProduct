using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLogin
{
	public class BasicLoginView : MonoBehaviour, ISyncBeamableView
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
		}

		[Header("View Configuration")]
		public int EnrichOrder;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			// TODO: bind the view deps to the prefab stuff.
		}
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
		/// When the user is adding an email association to their account
		/// </summary>
		EMAIL_LOGIN,

		/// <summary>
		/// When the user is creating a new anonymous account
		/// </summary>
		NEW_USER,

		THIRD_PARTY_LOGIN,
		ERROR,
		FORGOT_PASSWORD,
		FORGOT_PASSWORD_CONFIRM,
		SWITCH_USER,
		SET_DETAILS
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
		/// A set of third party association codes, if they exist.
		/// </summary>
		public string[] thirdPartyAssociations;

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
	}

	[Serializable]
	public class OptionalUserView : Optional<UserView>
	{
	}

}
