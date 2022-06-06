using System.Collections.Generic;
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
			Party Party { get; }
			bool IsPlayerLeader { get; }
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

		protected IDependencies System;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			System = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);
			if (!System.IsVisible)
			{
				return;
			}
			
			PartyIdText.text = System.Party.PartyId;

			LeadButtonsGroup.SetActive(System.IsPlayerLeader);
			NonLeadButtonsGroup.SetActive(!System.IsPlayerLeader);
			SettingsButton.gameObject.SetActive(System.IsPlayerLeader);

			// set callbacks
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			SettingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			CreateLobbyButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			JoinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			QuickStartButton.onClick.ReplaceOrAddListener(QuickStartButtonClicked);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(NextButtonClicked);

			// temporary players list
			List<PartySlotPresenter.ViewData> players = new List<PartySlotPresenter.ViewData>()
			{
				new PartySlotPresenter.ViewData {PlayerId = ctx.PlayerId.ToString()},
				new PartySlotPresenter.ViewData {PlayerId = "Test player"},
				new PartySlotPresenter.ViewData {PlayerId = "Test player"},
				new PartySlotPresenter.ViewData {PlayerId = ""},
			};
			PartyList.Setup(players, OnPlayerAccepted, OnAskedToLeave, OnPromoted, OnAddMember);
		}

		private void OnAddMember()
		{
			List<PartySlotPresenter.ViewData> friendsList = new List<PartySlotPresenter.ViewData>()
			{
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 1"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 2"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 3"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 4"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 5"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 6"},
				new PartySlotPresenter.ViewData() {PlayerId = "Test player 7"},
			};
			FeatureControl.OpenInviteView(friendsList, System.Party);
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = System.Party.PartyId;
			Debug.Log("Party ID copied to clipboard");
		}

		private void OnPromoted(string id)
		{
			throw new System.NotImplementedException();
		}

		private void OnAskedToLeave(string id)
		{
			throw new System.NotImplementedException();
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
			FeatureControl.OpenCreatePartyView(System.Party);
		}

		private void LeaveButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}
	}
}
