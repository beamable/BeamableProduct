using Beamable.Common;
using Beamable.Common.Player;
using Beamable.EasyFeatures.Components;
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
			Promise<List<AccountSlotPresenter.ViewData>> GetPlayersViewData(List<long> playerIds);
		}

		public enum View
		{
			Sent = 0,
			Received = 1,
		}

		public SocialFeatureControl FeatureControl;
		public TextMeshProUGUI PlayerIdText;
		public TMP_InputField PlayerIdInputField;
		public PlayerInviteUI InviteUI;
		public View DefaultView = View.Sent;
		public MultiToggleComponent Tabs;
		public AccountsListPresenter ReceivedListPresenter;
		public AccountsListPresenter SentListPresenter;
		public Button CopyIdButton;

		protected IDependencies System;

		private Dictionary<View, AccountsListPresenter> _views = new Dictionary<View, AccountsListPresenter>();
		private View _currentView;
		private AccountsListPresenter _currentListView;

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
			_views.Clear();
			_views.Add(View.Sent, SentListPresenter);
			_views.Add(View.Received, ReceivedListPresenter);
			
			Tabs.Setup(Enum.GetNames(typeof(View)).ToList(), OnTabSelected, (int)DefaultView);
			
			PlayerIdInputField.onSelect.ReplaceOrAddListener(OpenInviteOverlay);
			CopyIdButton.onClick.ReplaceOrAddListener(CopyPlayerId);
			
			await OpenTab(DefaultView);
		}

		private void OpenInviteOverlay(string ignore)
		{
			var overlay = FeatureControl.OverlaysController.ShowCustomOverlay(InviteUI);
			overlay.Setup(System);
		}

		private async void OnTabSelected(int tabId)
		{
			await OpenTab((View)tabId);
		}

		private void OnDisable()
		{
			if (System != null && System.Context.Social.OnReady.IsCompleted)
			{
				UnsubscribeFromInvitesEvents();	
			}
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

		private async Promise OpenTab(View view)
		{
			FeatureControl.SetLoadingOverlay(true);

			if (_currentListView != null)
			{
				_currentListView.gameObject.SetActive(false);
			}
			
			_currentView = view;
			_currentListView = _views[_currentView];
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

		private async Promise RefreshView() => await OpenTab(_currentView);

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
