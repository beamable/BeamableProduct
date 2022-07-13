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
			bool IsVisible { get; set; }
		}

		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		public TextMeshProUGUI TitleText;
		public PlayersListPresenter PartyList;
		public Button SettingsButton;
		public Button BackButton;
		public Button CreateButton;

		protected BeamContext Context;
		protected IDependencies System;

		public int GetEnrichOrder() => EnrichOrder;

		public async void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();
			System = Context.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(System.IsVisible);
			if (!System.IsVisible)
			{
				return;
			}

			TitleText.text = Context.PlayerId.ToString();
			
			// set callbacks
			SettingsButton.onClick.ReplaceOrAddListener(OnSettingsButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			CreateButton.onClick.ReplaceOrAddListener(OnCreateButtonClicked);
			
			// prepare friends list
			await Context.Friends.OnReady;	// show loading
			var friendsList = Context.Friends.FriendsList;
			string[] friends = new string[friendsList.Count];
			for (int i = 0; i < friends.Length; i++)
			{
				friends[i] = friendsList[i].playerId;
			}
			
			PartyList.Setup(friends.ToList(), true, OnPlayerInvited, null, null, null);
		}

		private void OnPlayerInvited(string id)
		{
			// send invite request
			OnBackButtonClicked();
		}

		private void OnCreateButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			if (Context.Party.IsInParty)
			{
				FeatureControl.OpenPartyView();
			}
		}

		private void OnSettingsButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
