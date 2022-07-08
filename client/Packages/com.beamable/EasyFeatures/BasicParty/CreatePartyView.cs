using Beamable.EasyFeatures.Components;
using Beamable.UI.Buss;
using EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

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
		public MultiToggleComponent AccessToggle;
		public Button CopyIdButton;
		public Button NextButton;
		public Button CancelButton;
		public Button BackButton;
		public BussElement NextButtonBussElement;

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
			Party.Players = CreateNewParty
				? new List<PartySlotPresenter.ViewData>
				{
					new PartySlotPresenter.ViewData
					{
						Avatar = null, IsReady = false, PlayerId = ctx.PlayerId.ToString()
					}
				}
				: Party.Players;
			HeaderText.text = CreateNewParty ? "CREATE" : "SETTINGS";
			PartyIdObject.gameObject.SetActive(!CreateNewParty);
			PartyIdInputField.text = CreateNewParty ? "" : Party.PartyId;
			MaxPlayersTextField.text = CreateNewParty ? "" : Party.MaxPlayers.ToString();
			MaxPlayersValueChanged(Party.MaxPlayers.ToString());
			AccessToggle.Setup(Enum.GetNames(typeof(PartyAccess)).ToList(), OnAccessOptionSelected, (int)Party.Access);

			// set callbacks
			MaxPlayersTextField.onValueChanged.ReplaceOrAddListener(MaxPlayersValueChanged);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			CancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void ValidateNextButton()
		{
			bool canCreateParty = System.ValidateConfirmButton(Party.MaxPlayers);

			NextButton.interactable = canCreateParty;

			if (canCreateParty)
			{
				NextButtonBussElement.SetButtonPrimary();
			}
			else
			{
				NextButtonBussElement.SetButtonDisabled();
			}
		}

		private void OnAccessOptionSelected(int optionId)
		{
			Party.Access = (PartyAccess)optionId;
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = Party.PartyId;
			FeatureControl.OverlaysController.ShowLabel("Party ID was copied", 3);
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				Party.MaxPlayers = maxPlayers;
				ValidateNextButton();
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
	}
}
