using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Parties;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyView : MonoBehaviour, ISyncBeamableView
	{
		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		[Header("Components")]
		public TextMeshProUGUI PartyIdText;

		public TextMeshProUGUI PlayerCountText;

		public PlayersListPresenter PartyList;
		public GameObject LeadButtonsGroup;
		public GameObject NonLeadButtonsGroup;

		[Header("Buttons")]
		public Button BackButton;

		public Button SettingsButton;
		public Button CreateLobbyButton;
		public Button JoinLobbyButton;
		public Button QuickStartButton;
		public Button CopyIdButton;
		public Button NextButton;
		public Button LeaveButton;

		protected BeamContext Context;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();

			if (!IsVisible)
			{
				return;
			}

			RefreshView();

			// set callbacks
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			SettingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			CreateLobbyButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			JoinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			QuickStartButton.onClick.ReplaceOrAddListener(QuickStartButtonClicked);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(NextButtonClicked);
			Context.Party.RegisterCallbacks(OnPlayerJoined, OnPlayerLeft, OnPartyUpdated,
											OnPlayerPromoted, OnPlayerKicked);
		}

		protected void RefreshView()
		{
			PartyIdText.text = Context.Party.Id;
			SetupPlayerCountText();

			LeadButtonsGroup.SetActive(Context.Party.IsLeader);
			NonLeadButtonsGroup.SetActive(!Context.Party.IsLeader);
			SettingsButton.gameObject.SetActive(Context.Party.IsLeader);

			SetupPartyList();
		}

		private void OnPartyUpdated(PartyUpdatedNotification notification)
		{
			RefreshView();
		}

		private void SetupPlayerCountText() =>
			PlayerCountText.text = $"{Context.Party.Members.Count}/{Context.Party.MaxSize}";

		private void SetupPartyList()
		{
			PartyList.Setup(Context.Party.Members.ToList(), Context.Party.IsLeader, null, OnAskedToLeave,
							OnPromoteButtonClicked, OnAddMember, Context.Party.MaxSize);
		}

		protected virtual void OnPlayerJoined(PlayerJoinedNotification notification)
		{
			RefreshView();
		}

		protected virtual void OnPlayerLeft(PlayerLeftNotification notification)
		{
			RefreshView();
		}

		private void OnAddMember()
		{
			FeatureControl.OpenInviteView();
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = Context.Party.Id;
			FeatureControl.OverlaysController.ShowToast("Party ID was copied");
		}

		private void OnPromoteButtonClicked(string id)
		{
			FeatureControl.OverlaysController.ShowConfirm($"Are you sure you want to transfer lead to {id}?",
														  () => PromotePlayer(id));
		}

		private async void PromotePlayer(string id)
		{
			await Context.Party.Promote(id);
		}

		private void OnPlayerPromoted(PlayerPromotedNotification notification)
		{
			RefreshView();

			if (notification.playerPromotedId.Equals(Context.PlayerId.ToString()))
			{
				FeatureControl.OverlaysController.ShowInform("You have been promoted to a party leader.", null);
			}
		}

		private void OnAskedToLeave(string id)
		{
			FeatureControl.OverlaysController.ShowConfirm($"Are you sure you want to ask {id} to leave the party?",
														  () => KickPlayer(id));
		}

		private async void KickPlayer(string id)
		{
			await Context.Party.Kick(id);
		}

		private void OnPlayerKicked(PlayerKickedNotification notification)
		{
			if (notification.kickedPlayerId.Equals(Context.PlayerId.ToString()))
			{
				FeatureControl.OpenJoinView();
				FeatureControl.OverlaysController.ShowInform("You have been kicked out from the party.", null);
			}
			else
			{
				RefreshView();
			}
		}

		private void NextButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void QuickStartButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void JoinLobbyButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void CreateLobbyButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void SettingsButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}

		private async void LeaveButtonClicked()
		{
			try
			{
				await Context.Party.Leave();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				throw;
			}
			finally
			{
				FeatureControl.OpenJoinView();
			}
		}
	}
}
