using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			List<PartySlotPresenter.ViewData> SlotsData { get; }
			bool IsVisible { get; }
			int MaxPlayers { get; }
		}

		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		[Header("Components")]
		public TextMeshProUGUI PartyIdText;

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
		protected IDependencies System;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();
			System = Context.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);
			if (!System.IsVisible)
			{
				return;
			}
			
			PartyIdText.text = Context.Party.Id;

			LeadButtonsGroup.SetActive(Context.Party.IsLeader);
			NonLeadButtonsGroup.SetActive(!Context.Party.IsLeader);
			SettingsButton.gameObject.SetActive(Context.Party.IsLeader);

			// set callbacks
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			SettingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			CreateLobbyButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			JoinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			QuickStartButton.onClick.ReplaceOrAddListener(QuickStartButtonClicked);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(NextButtonClicked);
			Context.Party.RegisterCallbacks(OnPlayerJoined, OnPlayerLeft);
			
			SetupPartyList();
		}

		private void SetupPartyList()
		{
			PartyList.Setup(Context.Party.Members.ToList(), Context.Party.IsLeader, OnPlayerAccepted, OnAskedToLeave, OnPromoted, OnAddMember, System.MaxPlayers);
		}

		protected virtual void OnPlayerJoined(object playerId)
		{
			SetupPartyList();
		}
		
		protected virtual void OnPlayerLeft(object playerId)
		{
			SetupPartyList();
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

		private void OnPromoted(string id)
		{
			FeatureControl.OverlaysController.ShowConfirm($"Are you sure you want to transfer lead to {id}?", () => PromotePlayer(id));
		}

		private async void PromotePlayer(string id)
		{
			await Context.Party.Promote(id);
		}

		private void OnAskedToLeave(string id)
		{
			// TODO Add confirm action once Party SDK is ready
			FeatureControl.OverlaysController.ShowConfirm($"Are you sure you want to ask {id} to leave the party?", null);
		}

		private void OnPlayerAccepted(string id)
		{
			throw new System.NotImplementedException();
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
			await Context.Party.Leave();
			FeatureControl.OpenJoinView();
		}
	}
}
