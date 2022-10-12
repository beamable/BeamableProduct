using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Parties;
using Beamable.UI.Buss;
using EasyFeatures.Components;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			int MaxPlayers { get; set; }
			PartyRestriction PartyRestriction { get; set; }
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

		protected BeamContext Context;
		protected IDependencies System;
		protected bool CreateNewParty;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();
			System = Context.ServiceProvider.GetService<IDependencies>();

			if (!IsVisible)
			{
				return;
			}

			CreateNewParty = !Context.Party.IsInParty;
			System.MaxPlayers = CreateNewParty ? 0 : Context.Party.MaxSize;
			HeaderText.text = CreateNewParty ? "CREATE" : "SETTINGS";
			PartyIdObject.gameObject.SetActive(!CreateNewParty);
			PartyIdInputField.text = CreateNewParty ? "" : Context.Party.Id;
			MaxPlayersTextField.text = CreateNewParty ? "" : System.MaxPlayers.ToString();
			MaxPlayersValueChanged(System.MaxPlayers.ToString());
			AccessToggle.Setup(Enum.GetNames(typeof(PartyRestriction)).ToList(), OnAccessOptionSelected, (int)System.PartyRestriction);

			// set callbacks
			MaxPlayersTextField.onValueChanged.ReplaceOrAddListener(MaxPlayersValueChanged);
			CopyIdButton.onClick.ReplaceOrAddListener(OnCopyIdButtonClicked);
			NextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			CancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void ValidateNextButton()
		{
			bool canCreateParty = System.ValidateConfirmButton(System.MaxPlayers);

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
			System.PartyRestriction = (PartyRestriction)optionId;
		}

		private void OnCopyIdButtonClicked()
		{
			GUIUtility.systemCopyBuffer = Context.Party.Id;
			FeatureControl.OverlaysController.ShowToast("Party ID was copied");
		}

		private void MaxPlayersValueChanged(string value)
		{
			if (int.TryParse(value, out int maxPlayers))
			{
				System.MaxPlayers = maxPlayers;
			}
			else
			{
				System.MaxPlayers = 0;
			}

			ValidateNextButton();
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
			if (!Context.Party.IsInParty)
			{
				FeatureControl.OpenJoinView();
			}

			FeatureControl.OpenPartyView();
		}

		private async void OnNextButtonClicked()
		{
			if (Context.Party.IsInParty)
			{
				// update party settings
				await Context.Party.Update(System.PartyRestriction, System.MaxPlayers);
			}
			else
			{
				// show loading
				await Context.Party.Create(System.PartyRestriction, System.MaxPlayers);
			}

			FeatureControl.OpenPartyView();
		}
	}
}
