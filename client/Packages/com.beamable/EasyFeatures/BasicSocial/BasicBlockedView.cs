using Beamable.Common;
using Beamable.Common.Player;
using Beamable.Player;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicBlockedView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			List<long> GetPlayersIds<T>(ObservableReadonlyList<T> list) where T : IPlayerId;
			Promise<List<FriendSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds);
		}
		
		public FriendsListPresenter BlockedListPresenter;

		protected IDependencies System;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder()
		{
			return 0;
		}

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;
			
			if (!IsVisible)
			{
				return;
			}

			await System.Context.Social.OnReady;

			List<long> blockedPlayers = System.GetPlayersIds(System.Context.Social.Blocked);
			var viewData = await System.GetPlayersViewData(blockedPlayers);

			BlockedListPresenter.Setup(viewData, UnblockPlayer, "Unblock");
		}

		private async void UnblockPlayer(long playerId)
		{
			await System.Context.Social.UnblockPlayer(playerId);
		}
	}
}
