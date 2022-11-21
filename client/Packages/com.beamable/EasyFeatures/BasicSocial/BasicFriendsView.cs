using Beamable.Common;
using Beamable.Common.Player;
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
			List<long> GetPlayersIds<T>(ObservableReadonlyList<T> list) where T : IPlayerId;
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

			System.Context.Social.Friends.OnDataUpdated += FriendsListUpdated;
			
			await SetupView();
		}

		private void OnDisable()
		{
			if (System != null && System.Context.Social.OnReady.IsCompleted)
			{
				System.Context.Social.Friends.OnDataUpdated -= FriendsListUpdated;	
			}
		}

		private async void FriendsListUpdated(List<PlayerFriend> friendsList)
		{
			await SetupView();
		}

		private async Promise SetupView()
		{
			List<long> friends = System.GetPlayersIds(System.Context.Social.Friends);
			var viewData = await System.GetPlayersViewData(friends);

			FriendsListPresenter.Setup(viewData, onEntryPressed: OnPlayerPressed);
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
			await SetupView();
		}

		private async void DeleteFriend(long playerId)
		{
			await System.Context.Social.Unfriend(playerId);
			await SetupView();
		}
	}
}
