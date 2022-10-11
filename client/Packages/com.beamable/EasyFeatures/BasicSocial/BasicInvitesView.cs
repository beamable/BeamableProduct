using Beamable.Common;
using Beamable.Common.Player;
using Beamable.Player;
using System;
using System.Collections.Generic;
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
			FeatureControl.SetLoadingOverlay(true);
			
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;

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
			FeatureControl.SetLoadingOverlay(true);
			_tabToggles[tab].isOn = true;
			
			if (_currentListView != null)
			{
				_currentListView.gameObject.SetActive(false);
			}

			_currentListView = tab;
			_currentListView.gameObject.SetActive(true);

			List<long> ids;
			Action<long> buttonAction;
			string buttonText;
			if (_currentListView == ReceivedListPresenter)
			{
				System.Context.Social.ReceivedInvites.OnElementsAdded -= OnInviteReceived;
				System.Context.Social.ReceivedInvites.OnElementsAdded += OnInviteReceived;
				ids = System.GetPlayersIds(System.Context.Social.ReceivedInvites);
				buttonAction = AcceptInviteFrom;
				buttonText = "Accept";
			}
			else
			{
				System.Context.Social.SentInvites.OnElementsAdded -= OnInviteSent;
				System.Context.Social.SentInvites.OnElementsAdded += OnInviteSent;
				ids = System.GetPlayersIds(System.Context.Social.SentInvites);
				buttonAction = null;
				buttonText = "";
			}

			var viewData = await System.GetPlayersViewData(ids);

			_currentListView.Setup(viewData, buttonAction, buttonText);
			
			FeatureControl.SetLoadingOverlay(false);
		}

		private async void OnInviteSent(IEnumerable<SentFriendInvite> sentInvites)
		{
			await RefreshView();
		}

		private async void OnInviteReceived(IEnumerable<ReceivedFriendInvite> receivedInvites)
		{
			await RefreshView();
		}

		private async Promise RefreshView() => await OpenTab(_currentListView);

		private async void AcceptInviteFrom(long invitingPlayerId)
		{
			await System.Context.Social.AcceptInviteFrom(invitingPlayerId);
			await RefreshView();
		}
	}
}
