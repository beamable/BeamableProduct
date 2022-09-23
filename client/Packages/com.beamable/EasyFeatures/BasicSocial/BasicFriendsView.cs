using Beamable.Common;
using Beamable.Player;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicFriendsView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			List<long> GetPlayersIds(PlayerFriendList list);
			Promise<List<FriendSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds);
		}
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public SocialFeatureControl FeatureControl;
		public int EnrichOrder = 0;

		public FriendsListPresenter FriendsListPresenter;

		protected IDependencies System;
		
		public int GetEnrichOrder() => EnrichOrder;

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

			List<long> friends = System.GetPlayersIds(System.Context.Social.Friends);
			var viewData = await System.GetPlayersViewData(friends);

			FriendsListPresenter.Setup(viewData, OnPlayerPressed);
		}

		private async void OnPlayerPressed(long playerId)
		{
			await FeatureControl.OpenInfoPopup(playerId, DeleteFriend, BlockPlayer, SendMessageTo);
		}

		private void SendMessageTo(long playerId)
		{
			throw new System.NotImplementedException();
		}

		private async void BlockPlayer(long playerId)
		{
			await System.Context.Social.BlockPlayer(playerId);
		}

		private async void DeleteFriend(long playerId)
		{
			await System.Context.Social.Unfriend(playerId);
		}
	}
}
