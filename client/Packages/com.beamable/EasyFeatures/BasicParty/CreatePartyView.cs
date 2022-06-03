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
			bool ValidateConfirmButton();
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
		private bool _isSettingsView;

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
			
			_isSettingsView = _system.Party == null;
			if (_isSettingsView)
			{
				_system.Party = new Party();
			}
			_headerText.text = _isSettingsView ? "CREATE" : "SETTINGS";
			_partyIdObject.gameObject.SetActive(!_isSettingsView);
			_partyIdInputField.text = _isSettingsView ? "" : _system.Party.PartyId;
			_maxPlayersTextField.text = _isSettingsView ? "" : _system.Party.MaxPlayers.ToString();
			_publicAccessToggle.isOn = _isSettingsView || _system.Party.Access == PartyAccess.Public;
			PartyAccessChanged(_publicAccessToggle.isOn);
			MaxPlayersValueChanged(_system.Party.MaxPlayers.ToString());
			
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
			GUIUtility.systemCopyBuffer = _system.Party.PartyId;
			Debug.Log("Party ID copied to clipboard");
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				_system.Party.MaxPlayers = maxPlayers;
				_nextButton.interactable = _system.ValidateConfirmButton();
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
			if (string.IsNullOrWhiteSpace(_system.Party.PartyId))
			{
				// placeholder party ID generation
				_system.Party.PartyId = Random.Range(10000, 99999).ToString();
			}
			
			FeatureControl.OpenPartyView(_system.Party);
		}
		
		private void PartyAccessChanged(bool isPublic)
		{
			_system.Party.Access = isPublic ? PartyAccess.Public : PartyAccess.Private;
		}
	}
}
