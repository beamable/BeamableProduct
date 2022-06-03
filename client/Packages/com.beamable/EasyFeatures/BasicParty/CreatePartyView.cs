using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			Party Party { get; set; }
			bool IsVisible { get; set; }
			bool ValidateConfirmButton(int maxPlayers);
		}

		public PartyFeatureControl FeatureControl;
		[SerializeField] private int _enrichOrder;

		[Header("Components")]
		[SerializeField] private TextMeshProUGUI _headerText;
		[SerializeField] private GameObject _partyIdObject;
		[SerializeField] private TMP_InputField _partyIdInputField;
		[SerializeField] private TMP_InputField _maxPlayersTextField;
		[SerializeField] private Toggle _publicAccessToggle;
		[SerializeField] private Button _copyIdButton;
		[SerializeField] private Button _nextButton;
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _backButton;

		private IDependencies _system;
		private bool _createNewParty;
		private Party _party;

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

			_createNewParty = _system.Party == null;
			_party = _createNewParty ? new Party() : _system.Party.Clone() as Party;
			_headerText.text = _createNewParty ? "CREATE" : "SETTINGS";
			_partyIdObject.gameObject.SetActive(!_createNewParty);
			_partyIdInputField.text = _createNewParty ? "" : _party.PartyId;
			_maxPlayersTextField.text = _createNewParty ? "" : _party.MaxPlayers.ToString();
			_publicAccessToggle.isOn = _createNewParty || _party.Access == PartyAccess.Public;
			PartyAccessChanged(_publicAccessToggle.isOn);
			MaxPlayersValueChanged(_party.MaxPlayers.ToString());
			
			// set callbacks
			_maxPlayersTextField.onValueChanged.ReplaceOrAddListener(MaxPlayersValueChanged);
			_publicAccessToggle.onValueChanged.ReplaceOrAddListener(PartyAccessChanged);
			_copyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			_nextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			_cancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = _party.PartyId;
			Debug.Log("Party ID copied to clipboard");
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				_party.MaxPlayers = maxPlayers;
				_nextButton.interactable = _system.ValidateConfirmButton(_party.MaxPlayers);
			}
		}

		private void OnBackButtonClicked()
		{
			ReturnToPartyView();
		}
		
		private void OnCancelButtonClicked()
		{
			ReturnToPartyView();
		}

		private void ReturnToPartyView()
		{
			if (_system.Party != null && !string.IsNullOrWhiteSpace(_system.Party.PartyId))
			{
				FeatureControl.OpenPartyView(_system.Party);
			}
		}

		private void OnNextButtonClicked()
		{
			if (string.IsNullOrWhiteSpace(_party.PartyId))
			{
				// placeholder party ID generation
				_party.PartyId = Random.Range(10000, 99999).ToString();
			}

			_system.Party = _party;			
			FeatureControl.OpenPartyView(_system.Party);
		}
		
		private void PartyAccessChanged(bool isPublic)
		{
			_party.Access = isPublic ? PartyAccess.Public : PartyAccess.Private;
		}
	}
}
