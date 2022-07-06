using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class InvitePlayersView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			Party Party { get; set; }
			bool IsVisible { get; set; }
			List<PartySlotPresenter.ViewData> FriendsList { get; set; }
		}

		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		public TextMeshProUGUI TitleText;
		public PlayersListPresenter PartyList;
		public Button SettingsButton;
		public Button BackButton;
		public Button CreateButton;
		
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

			TitleText.text = ctx.PlayerId.ToString();
			
			// set callbacks
			SettingsButton.onClick.ReplaceOrAddListener(OnSettingsButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			CreateButton.onClick.ReplaceOrAddListener(OnCreateButtonClicked);
			
			PartyList.Setup(System.FriendsList, true, OnPlayerInvited, null, null, null);
		}

		private void OnPlayerInvited(string id)
		{
			PartySlotPresenter.ViewData newPlayer = new PartySlotPresenter.ViewData
			{
				Avatar = null, IsReady = false, PlayerId = id
			};
			System.Party.Players.Add(newPlayer);
			OnBackButtonClicked();
		}

		private void OnCreateButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			if (System.Party != null)
			{
				FeatureControl.OpenPartyView(System.Party);
			}
		}

		private void OnSettingsButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
