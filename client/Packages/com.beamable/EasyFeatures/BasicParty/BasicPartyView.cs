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
		[SerializeField] private int _enrichOrder;

		[Header("Components")]
		[SerializeField] private TextMeshProUGUI _partyIdText;

		[SerializeField] private PlayersListPresenter _partyList;
		[SerializeField] private GameObject _leadButtonsGroup;
		[SerializeField] private GameObject _nonLeadButtonsGroup;

		[Header("Buttons")]
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _createLobbyButton;
		[SerializeField] private Button _joinLobbyButton;
		[SerializeField] private Button _quickStartButton;
		[SerializeField] private Button _copyIdButton;
		[SerializeField] private Button _nextButton;
		[SerializeField] private Button _leaveButton;

		private IDependencies _system;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			_system = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(_system.IsVisible);
			if (!_system.IsVisible)
			{
				return;
			}
			
			_partyIdText.text = _system.Party.PartyId;

			_leadButtonsGroup.SetActive(_system.IsPlayerLeader);
			_nonLeadButtonsGroup.SetActive(!_system.IsPlayerLeader);
			_settingsButton.gameObject.SetActive(_system.IsPlayerLeader);

			// set callbacks
			_backButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			_leaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			_settingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			_createLobbyButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			_joinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			_quickStartButton.onClick.ReplaceOrAddListener(QuickStartButtonClicked);
			_copyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			_nextButton.onClick.ReplaceOrAddListener(NextButtonClicked);

			// temporary players list
			List<PartySlotPresenter.ViewData> players = new List<PartySlotPresenter.ViewData>()
			{
				new PartySlotPresenter.ViewData {PlayerId = ctx.PlayerId.ToString()},
				new PartySlotPresenter.ViewData {PlayerId = "Test player"},
				new PartySlotPresenter.ViewData {PlayerId = "Test player"},
				new PartySlotPresenter.ViewData {PlayerId = ""},
			};
			_partyList.Setup(players, OnPlayerAccepted, OnAskedToLeave, OnPromoted, OnAddMember);
		}

		private void OnAddMember()
		{
			FeatureControl.OpenInviteView(new List<PartySlotPresenter.ViewData>());
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = _system.Party.PartyId;
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
			FeatureControl.OpenCreatePartyView(_system.Party);
		}

		private void LeaveButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}
	}
}
