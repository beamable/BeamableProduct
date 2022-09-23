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
			string PartyIdToJoin { get; set; }
			bool ValidateJoinButton();
		}

		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		public TMP_InputField PartyIdInputField;
		public Button BackButton;
		public Button JoinButton;
		public Button CancelButton;
		public BussElement JoinButtonBussElement;

		protected BeamContext Context;
		protected IDependencies System;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();
			System = Context.ServiceProvider.GetService<IDependencies>();

			if (!IsVisible)
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
			System.PartyIdToJoin = value;
			ValidateJoinButton();
		}

		private void OnCancelButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}

		private void OnBackButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}

		private async void OnJoinButtonClicked()
		{
			await Context.Party.Join(System.PartyIdToJoin); // add loading
			FeatureControl.OpenPartyView();
		}
	}
}
