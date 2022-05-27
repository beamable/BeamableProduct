using Beamable.Common.Api.Auth;
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
			/// The currently signed in user.
			/// </summary>
			UserView CurrentUser { get; }

			/// <summary>
			/// A list of other users that exist on the device, and could be set to the current user.
			/// </summary>
			List<UserView> AvailableUsers { get; }
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

}
