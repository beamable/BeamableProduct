using Beamable.Avatars;
using Beamable.Common;
using Beamable.EasyFeatures.Components;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountManagementPlayerSystem : CurrentAccountView.IDependencies
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
				PlayerId = playerId,
				PlayerName = playerName,
				Avatar = avatar,
				Description = "Description"
			};
			
			return data;
		}
	}
}
