using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Player;
using Beamable.Player;
using System;
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
			List<long> GetPlayersIds<T>(ObservableReadonlyList<T> list) where T : IPlayerId;
			Promise<List<FriendSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds);
		}

		public SocialFeatureControl FeatureControl;
		public TextMeshProUGUI PlayerIdText;
		public TMP_InputField PlayerIdInputField;
		public Toggle PendingListToggle;
		public Toggle SentListToggle;
		public FriendsListPresenter ReceivedListPresenter;
		public FriendsListPresenter SentListPresenter;
		public Button CopyIdButton;

		protected IDependencies System;

		private FriendsListPresenter _currentListView;
		private Dictionary<FriendsListPresenter, Toggle> _tabToggles = new Dictionary<FriendsListPresenter, Toggle>();

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
			
			FeatureControl.SetLoadingOverlay(true);
			
			await System.Context.Social.OnReady;

			PlayerIdText.text = $"#{context.PlayerId}";
			ReceivedListPresenter.gameObject.SetActive(false);
			SentListPresenter.gameObject.SetActive(false);
			_tabToggles.Clear();
			_tabToggles.Add(ReceivedListPresenter, PendingListToggle);
			_tabToggles.Add(SentListPresenter, SentListToggle);
			
			PendingListToggle.onValueChanged.ReplaceOrAddListener(isOn => TabPicked(isOn, ReceivedListPresenter));
			SentListToggle.onValueChanged.ReplaceOrAddListener(isOn => TabPicked(isOn, SentListPresenter));
			PlayerIdInputField.onEndEdit.ReplaceOrAddListener(SendInvite);
			CopyIdButton.onClick.ReplaceOrAddListener(CopyPlayerId);
			
			await OpenTab(ReceivedListPresenter);
		}

		private void OnDisable()
		{
			UnsubscribeFromInvitesEvents();
		}

		private void UnsubscribeFromInvitesEvents()
		{
			System.Context.Social.ReceivedInvites.OnDataUpdated -= OnReceivedListChanged;
			System.Context.Social.SentInvites.OnDataUpdated -= OnSentListChanged;
		}

		private void CopyPlayerId()
		{
			GUIUtility.systemCopyBuffer = System.Context.PlayerId.ToString();
		}

		private async void SendInvite(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				return;
			}

			try
			{
				await System.Context.Social.Invite(id);
				FeatureControl.OverlaysController.ShowInform($"Sent invite to player {playerId}", null);
			}
			catch (PlatformRequesterException e)
			{
				if (e.Error.status == 404)
				{
					FeatureControl.OverlaysController.ShowError($"No player found with id {playerId}");
				}
				else
				{
					throw;
				}
			}
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
			FeatureControl.SetLoadingOverlay(true);
			_tabToggles[tab].isOn = true;
			
			if (_currentListView != null)
			{
				_currentListView.gameObject.SetActive(false);
			}

			_currentListView = tab;
			_currentListView.gameObject.SetActive(true);

			UnsubscribeFromInvitesEvents();
			
			if (_currentListView == ReceivedListPresenter)
			{
				System.Context.Social.ReceivedInvites.OnDataUpdated += OnReceivedListChanged;
				var ids = System.GetPlayersIds(System.Context.Social.ReceivedInvites);
				var viewData = await System.GetPlayersViewData(ids);
				_currentListView.Setup(viewData, AcceptInviteFrom, CancelInviteFrom);
			}
			else
			{
				System.Context.Social.SentInvites.OnDataUpdated += OnSentListChanged;
				var ids = System.GetPlayersIds(System.Context.Social.SentInvites);
				var viewData = await System.GetPlayersViewData(ids);
				_currentListView.Setup(viewData, CancelSentInviteTo, "Recall");
			}

			FeatureControl.SetLoadingOverlay(false);
		}

		private async void OnSentListChanged(IEnumerable<SentFriendInvite> sentInvites)
		{
			await RefreshView();
		}

		private async void OnReceivedListChanged(IEnumerable<ReceivedFriendInvite> receivedInvites)
		{
			await RefreshView();
		}

		private async Promise RefreshView() => await OpenTab(_currentListView);

		private async void AcceptInviteFrom(long invitingPlayerId)
		{
			await System.Context.Social.AcceptInviteFrom(invitingPlayerId);
			await RefreshView();
		}
		
		private void CancelInviteFrom(long playerId)
		{
			throw new NotImplementedException();
		}
		
		private async void CancelSentInviteTo(long playerId)
		{
			var sentInvite = System.Context.Social.SentInvites.FirstOrDefault(invite => invite.PlayerId == playerId);
			if (sentInvite != null)
			{
				await sentInvite.Cancel();
			}
			else
			{
				Debug.LogError($"There is no invite sent to player {playerId}");
			}
		}
	}
}
