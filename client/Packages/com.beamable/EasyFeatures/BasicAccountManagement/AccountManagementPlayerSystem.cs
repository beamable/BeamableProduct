using Beamable.Avatars;
using Beamable.Common;
using Beamable.EasyFeatures.Components;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountManagementPlayerSystem : AccountsView.IDependencies, CreateAccountView.IDependencies,
	                                             SignInView.IDependencies, ForgotPasswordView.IDependencies,
	                                             AccountInfoView.IDependencies
	{
		public BeamContext Context { get; set; }

		public async Promise<AccountSlotPresenter.ViewData> GetAccountViewData()
		{
			long playerId = Context.PlayerId;
			var stats = await Context.Api.StatsService.GetStats("client", "public", "player", playerId);
			if (!stats.TryGetValue("alias", out string playerName))
			{
				playerName = playerId.ToString();
			}

			Sprite avatar = null;
			if (stats.TryGetValue("avatar", out string avatarName))
			{
				var accountAvatar = AvatarConfiguration.Instance.Avatars.Find(av => av.Name == avatarName);
				if (accountAvatar != null)
				{
					avatar = accountAvatar.Sprite;
				}
			}

			var data = new AccountSlotPresenter.ViewData
			{
				PlayerId = playerId, PlayerName = playerName, Avatar = avatar, Description = "Description"
			};

			return data;
		}

		public bool IsAccountDataValid(string email, string password, string confirmPassword, out string errorMessage)
		{
			if (!IsEmailValid(email, out errorMessage))
			{
				return false;
			}

			return IsPasswordValid(password, confirmPassword, out errorMessage);
		}

		public bool IsEmailValid(string email, out string errorMessage)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				errorMessage = "You must provide an email address";
				return false;
			}

			if (!Regex.IsMatch(email, @"^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*@[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$"))
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
