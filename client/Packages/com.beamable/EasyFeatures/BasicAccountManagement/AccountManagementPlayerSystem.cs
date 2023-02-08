using Beamable.Avatars;
using Beamable.Common;
using Beamable.Common.Api.Presence;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountManagementPlayerSystem : AccountsView.IDependencies, CreateAccountView.IDependencies,
	                                             SignInView.IDependencies, ForgotPasswordView.IDependencies,
	                                             AccountInfoView.IDependencies
	{
		public BeamContext Context { get; set; }
		public string Email { get; set; }

		private async Promise<Dictionary<string, string>> GetPublicStats(long playerId) =>
			await Context.Api.StatsService.GetStats("client", "public", "player", playerId);

		public async Promise<string> GetCurrentAvatarName(long playerId)
		{
			var stats = await GetPublicStats(playerId);
			stats.TryGetValue("avatar", out string avatarName);
			return avatarName;
		}

		public async Promise SetAvatar(string avatarName)
		{
			await Context.Accounts.SetAvatar(avatarName);
		}

		/// <summary>
		/// Provides the current username of the given player. Returns an empty string if no username was set.
		/// </summary>
		public async Promise<string> GetUsername(long playerId)
		{
			var stats = await GetPublicStats(playerId);
			if (stats.TryGetValue("alias", out string username))
			{
				return username;
			}

			return string.Empty;
		}

		public async Promise SetUsername(string username)
		{
			await Context.Accounts.SetAlias(username);
		}

		public async Promise<PlayerPresence> GetOnlineStatus()
		{
			return await Context.Presence.GetPlayerPresence(Context.PlayerId);
		}

		/// <summary>
		/// Gets account view data for a given player. Default parameter will return current user's view data.
		/// </summary>
		public async Promise<AccountSlotPresenter.ViewData> GetAccountViewData(long playerId = -1)
		{
			playerId = playerId == -1 ? Context.PlayerId : playerId;
			var stats = await Context.Api.StatsService.GetStats("client", "public", "player", playerId);
			if (!stats.TryGetValue("alias", out string playerName))
			{
				playerName = "Anonymous";
			}

			Sprite avatar = null;
			string avatarName = await GetCurrentAvatarName(playerId);
			var accountAvatar = AvatarConfiguration.Instance.Avatars.Find(av => av.Name == avatarName);
			if (accountAvatar != null)
			{
				avatar = accountAvatar.Sprite;
			}

			string description = "";
			var account = Context.Accounts.FirstOrDefault(acc => acc.GamerTag == playerId);
			if (account != null)
			{
				description = account.HasEmail ? account.Email : "No email linked";
			}

			var data = new AccountSlotPresenter.ViewData
			{
				PlayerId = playerId, PlayerName = playerName, Avatar = avatar, Description = description
			};

			return data;
		}

		public int AuthenticatedAccountsCount()
		{
			return Context.Accounts.Count(acc => acc.HasEmail ||
			                                     (acc.ThirdParties != null && acc.ThirdParties.Length > 0));
		}

		/// <summary>
		/// Gets a linked email address for a given player or an empty string if there's no linked email.
		/// </summary>
		public string GetLinkedEmailAddress(long playerId)
		{
			var account = Context.Accounts.FirstOrDefault(acc => acc.GamerTag == playerId);
			if (account != null && account.HasEmail)
			{
				return account.Email;
			}

			return string.Empty;
		}

		public bool IsEmailValid(string email, out string errorMessage)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				errorMessage = "You must provide an email address";
				return false;
			}

			bool hasLessThan3Characters = email.Length < 3;
			bool hasOneAtCharacter = email.Count(c => c == '@') == 1;
			bool atCharacterIsInTheMiddle = email.First() != '@' && email.Last() != '@';
			if (hasLessThan3Characters || !hasOneAtCharacter || !atCharacterIsInTheMiddle)
			{
				errorMessage = "Email address is incorrect";
				return false;
			}

			errorMessage = "";
			return true;
		}

		public bool IsPasswordValid(string password, string confirmation, out string errorMessage)
		{
			if (string.IsNullOrWhiteSpace(password))
			{
				errorMessage = "You must provide a password";
				return false;
			}

			if (confirmation != password)
			{
				errorMessage = "Passwords don't match";
				return false;
			}

			errorMessage = "";
			return true;
		}
	}
}
