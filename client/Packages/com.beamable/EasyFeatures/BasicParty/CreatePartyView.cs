using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
		public int _enrichOrder;

		[Header("Components")]
		public TextMeshProUGUI HeaderText;
		public GameObject PartyIdObject;
		public TMP_InputField PartyIdInputField;
		public TMP_InputField MaxPlayersTextField;
		public Toggle PublicAccessToggle;
		public Button CopyIdButton;
		public Button NextButton;
		public Button CancelButton;
		public Button BackButton;

		protected IDependencies System;
		protected bool CreateNewParty;
		protected Party Party;

		public int GetEnrichOrder() => _enrichOrder;
		
		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			System = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(System.IsVisible);
			if (!System.IsVisible)
			{
				return;
			}

			CreateNewParty = System.Party == null;
			Party = CreateNewParty ? new Party() : System.Party.Clone() as Party;
			HeaderText.text = CreateNewParty ? "CREATE" : "SETTINGS";
			PartyIdObject.gameObject.SetActive(!CreateNewParty);
			PartyIdInputField.text = CreateNewParty ? "" : Party.PartyId;
			MaxPlayersTextField.text = CreateNewParty ? "" : Party.MaxPlayers.ToString();
			PublicAccessToggle.isOn = CreateNewParty || Party.Access == PartyAccess.Public;
			PartyAccessChanged(PublicAccessToggle.isOn);
			MaxPlayersValueChanged(Party.MaxPlayers.ToString());
			
			// set callbacks
			MaxPlayersTextField.onValueChanged.ReplaceOrAddListener(MaxPlayersValueChanged);
			PublicAccessToggle.onValueChanged.ReplaceOrAddListener(PartyAccessChanged);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			CancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = Party.PartyId;
			Debug.Log("Party ID copied to clipboard");
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				Party.MaxPlayers = maxPlayers;
				NextButton.interactable = System.ValidateConfirmButton(Party.MaxPlayers);
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
			if (System.Party != null && !string.IsNullOrWhiteSpace(System.Party.PartyId))
			{
				FeatureControl.OpenPartyView(System.Party);
			}
		}

		private void OnNextButtonClicked()
		{
			if (string.IsNullOrWhiteSpace(Party.PartyId))
			{
				// placeholder party ID generation
				Party.PartyId = Random.Range(10000, 99999).ToString();
			}

			System.Party = Party;			
			FeatureControl.OpenPartyView(System.Party);
		}
		
		private void PartyAccessChanged(bool isPublic)
		{
			Party.Access = isPublic ? PartyAccess.Public : PartyAccess.Private;
		}
	}
}
