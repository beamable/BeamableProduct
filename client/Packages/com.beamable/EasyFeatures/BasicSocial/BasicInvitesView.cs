using Beamable.Common;
using Beamable.Common.Player;
using Beamable.Player;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicInvitesView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			List<long> GetPlayersIds(ReceivedFriendInviteList list);
			List<long> GetPlayersIds<T>(ObservableReadonlyList<T> list);
			Promise<List<FriendSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds);
		}
		
		public TextMeshProUGUI PlayerIdText;
		public TMP_InputField PlayerIdInputField;
		public Toggle PendingListToggle;
		public Toggle SentListToggle;
		public FriendsListPresenter PendingListPresenter;
		public FriendsListPresenter SentListPresenter;

		protected IDependencies System;
		
		private FriendsListPresenter _currentListView;
		
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
			
			await System.Context.Social.OnReady;

			PlayerIdText.text = $"#{context.PlayerId}";
			PendingListPresenter.gameObject.SetActive(false);
			SentListPresenter.gameObject.SetActive(false);
			
			PendingListToggle.onValueChanged.AddListener(isOn => TabPicked(isOn, PendingListPresenter));
			SentListToggle.onValueChanged.AddListener(isOn => TabPicked(isOn, SentListPresenter));
			PlayerIdInputField.onEndEdit.AddListener(SendInvite);
			
			await OpenTab(PendingListPresenter);
		}

		private async void SendInvite(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				return;
			}
			
			await System.Context.Social.Invite(id);
		}

		private async void TabPicked(bool isOn, FriendsListPresenter list)
		{
			if (!isOn)
			{
				return;
			}
			
			await OpenTab(list);
		}

		private async Promise OpenTab(FriendsListPresenter tab)
		{
			if (_currentListView != null)
			{
				_currentListView.gameObject.SetActive(false);
			}

			_currentListView = tab;
			_currentListView.gameObject.SetActive(true);

			if (_currentListView == PendingListPresenter)
			{
				
			}
			else
			{
				
			}

			var blockedPlayers = System.GetPlayersIds(System.Context.Social.Blocked);
			var viewData = await System.GetPlayersViewData(blockedPlayers);
			
			_currentListView.Setup(viewData);
		}
	}
}
