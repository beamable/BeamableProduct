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
			bool IsVisible { get; set; }
			string PartyId { get; set; }
			int MaxPlayers { get; set; }
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
		[SerializeField] private Button _nextButton;
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _backButton;

		private PartyAccess _access;
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
			
			PartyAccessChanged(_publicAccessToggle.isOn);
			bool createNew = string.IsNullOrWhiteSpace(_system.PartyId);
			_partyIdObject.gameObject.SetActive(!createNew);
			_partyIdInputField.text = _system.PartyId;
			_headerText.text = createNew ? "CREATE" : "SETTINGS";
			
			// set callbacks
			_maxPlayersTextField.onValueChanged.ReplaceOrAddListener(MaxPlayersValueChanged);
			_publicAccessToggle.onValueChanged.ReplaceOrAddListener(PartyAccessChanged);
			_nextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			_cancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				_system.MaxPlayers = maxPlayers;
				_nextButton.interactable = _system.ValidateConfirmButton();
			}
		}

		private void OnBackButtonClicked()
		{
			throw new System.NotImplementedException();
		}
		
		private void OnCancelButtonClicked()
		{
			
		}

		private void OnNextButtonClicked()
		{
			if (string.IsNullOrWhiteSpace(_system.PartyId))
			{
				_system.PartyId = Random.Range(10000, 99999).ToString();
			}

			PartyData data = new PartyData
			{
				Access = _access, MaxPlayers = _system.MaxPlayers, PartyId = _system.PartyId,
			};
			
			FeatureControl.OpenPartyView(data);
		}
		
		private void PartyAccessChanged(bool isPublic)
		{
			_access = isPublic ? PartyAccess.Public : PartyAccess.Private;
		}
	}
}
