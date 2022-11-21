using Beamable.Avatars;
using Beamable.Common;
using Beamable.Common.Player;
using Beamable.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicSocialPlayerSystem : BasicFriendsView.IDependencies, BasicBlockedView.IDependencies, BasicInvitesView.IDependencies
	{
		public BeamContext Context { get; set; }

		public List<long> GetPlayersIds<T>(ObservableReadonlyList<T> list) where T : IPlayerId
		{
			List<long> ids = new List<long>(list.Count);
			foreach (var player in list)
			{
				ids.Add(player.PlayerId);
			}
		
			return ids;
		}

		public async Promise<List<FriendSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds)
		{
			FriendSlotPresenter.ViewData[] viewData = new FriendSlotPresenter.ViewData[playerIds.Count];
			for (int i = 0; i < playerIds.Count; i++)
			{
				var stats = await Context.Api.StatsService.GetStats("client", "public", "player", playerIds[i]);
				if (!stats.TryGetValue("alias", out string playerName))
				{
					playerName = playerIds[i].ToString();
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

				viewData[i] = new FriendSlotPresenter.ViewData
				{
					PlayerId = playerIds[i],
					PlayerName = playerName,
					Avatar = avatar,
					Description = "Description"
				};
			}

			return viewData.ToList();
		}
	}
}
