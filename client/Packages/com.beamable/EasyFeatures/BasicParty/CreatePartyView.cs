using Beamable.Avatars;
using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Parties;
using Beamable.UI.Buss;
using EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
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

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();
			System = Context.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);
			if (!System.IsVisible)
			{
				return;
			}

			CreateNewParty = Context.Party.IsInParty;
			Party.Players = CreateNewParty
				? new List<PartySlotPresenter.ViewData>
				{
					new PartySlotPresenter.ViewData
					{
						Avatar = AvatarConfiguration.Instance.Default.Sprite, IsReady = false, PlayerId = Context.PlayerId.ToString()
					}
				}
				: Party.Players;
			HeaderText.text = CreateNewParty ? "CREATE" : "SETTINGS";
			PartyIdObject.gameObject.SetActive(!CreateNewParty);
			PartyIdInputField.text = CreateNewParty ? "" : Party.PartyId;
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
				System.MaxPlayers = maxPlayers;
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

		private async void OnNextButtonClicked()
		{
			// show loading
			await Context.Party.Create(PartyRestriction.Unrestricted, OnPlayerJoined, OnPlayerJoined);

			System.Party = Context.Party.State;
			FeatureControl.OpenPartyView(System.Party);
		}

		private void OnPlayerJoined(object obj)
		{
			throw new NotImplementedException();
		}
	}
}
