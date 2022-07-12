using Beamable.Avatars;
using System.Linq;
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

		public async void EnrichWithContext(BeamContextGroup managedPlayers)
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
			
			// prepare players view data
			await ctx.Friends.OnReady;	// show loading
			var friendsList = ctx.Friends.FriendsList;
			PartySlotPresenter.ViewData[] viewData = new PartySlotPresenter.ViewData[friendsList.Count];
			for (int i = 0; i < viewData.Length; i++)
			{
				viewData[i] = new PartySlotPresenter.ViewData
				{
					Avatar = AvatarConfiguration.Instance.Default.Sprite, IsReady = false, PlayerId = friendsList[i].playerId
				};
			}
			
			PartyList.Setup(viewData.ToList(), true, OnPlayerInvited, null, null, null);
		}

		private void OnPlayerInvited(string id)
		{
			PartySlotPresenter.ViewData newPlayer = new PartySlotPresenter.ViewData
			{
				Avatar = AvatarConfiguration.Instance.Default.Sprite, IsReady = false, PlayerId = id
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
