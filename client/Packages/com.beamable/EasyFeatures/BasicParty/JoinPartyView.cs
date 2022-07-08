using Beamable.UI.Buss;
using EasyFeatures.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class JoinPartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			string PartyId { get; set; }
			bool IsVisible { get; set; }
			bool ValidateJoinButton();
		}

		public int EnrichOrder;

		public TMP_InputField PartyIdInputField;
		public Button BackButton;
		public Button JoinButton;
		public Button CancelButton;
		public BussElement JoinButtonBussElement;
		
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
			
			OnPartyIdValueChanged(PartyIdInputField.text);
			
			PartyIdInputField.onValueChanged.ReplaceOrAddListener(OnPartyIdValueChanged);
			JoinButton.onClick.ReplaceOrAddListener(OnJoinButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			CancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
		}

		private void ValidateJoinButton()
		{
			bool canJoinParty = System.ValidateJoinButton();

			JoinButton.interactable = canJoinParty;

			if (canJoinParty)
			{
				JoinButtonBussElement.SetButtonPrimary();
			}
			else
			{
				JoinButtonBussElement.SetButtonDisabled();
			}
		}

		private void OnPartyIdValueChanged(string value)
		{
			System.PartyId = value;
			ValidateJoinButton();
		}

		private void OnCancelButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnJoinButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
