using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			PartyData PartyData { get; }
			bool IsPlayerLeader { get; }
		}

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
		[SerializeField] private Button _invitePlayerButton;
		[SerializeField] private Button _nextButton;
		[SerializeField] private Button _leaveButton;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			var system = ctx.ServiceProvider.GetService<IDependencies>();

			_partyIdText.text = system.PartyData.PartyId;

			_leadButtonsGroup.SetActive(system.IsPlayerLeader);
			_nonLeadButtonsGroup.SetActive(!system.IsPlayerLeader);
			_settingsButton.gameObject.SetActive(system.IsPlayerLeader);

			// set callbacks
			_backButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			_leaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			_settingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			_createLobbyButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			_joinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			_quickStartButton.onClick.ReplaceOrAddListener(QuickStartButtonClicked);
			_invitePlayerButton.onClick.ReplaceOrAddListener(InvitePlayerButtonClicked);
			_nextButton.onClick.ReplaceOrAddListener(NextButtonClicked);
			
			_partyList.Setup(OnPlayerAccepted);
		}

		private void OnPlayerAccepted(string id)
		{
			
		}

		private void NextButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void InvitePlayerButtonClicked()
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
			throw new System.NotImplementedException();
		}

		private void LeaveButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
