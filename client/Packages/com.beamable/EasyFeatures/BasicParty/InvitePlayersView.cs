using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class InvitePlayersView : MonoBehaviour, ISyncBeamableView
	{
		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		public TextMeshProUGUI TitleText;
		public PlayersListPresenter PartyList;
		public Button SettingsButton;
		public Button BackButton;
		public Button CreateButton;

		protected BeamContext Context;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public async void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();

			if (!IsVisible)
			{
				return;
			}

			TitleText.text = Context.PlayerId.ToString();

			// set callbacks
			SettingsButton.onClick.ReplaceOrAddListener(OnSettingsButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			CreateButton.onClick.ReplaceOrAddListener(OnCreateButtonClicked);

			// prepare friends list
			await Context.Social.OnReady;   // show loading
			var friendsList = Context.Social.Friends;
			// string[] friends = new string[friendsList.Count];
			List<string> friends = new List<string>(friendsList.Count);
			for (int i = 0; i < friendsList.Count; i++)
			{
				if (Context.Party.Members.Any(playerId => playerId.Equals(friendsList[i].playerId.ToString())))
					continue;

				friends.Add(friendsList[i].playerId.ToString());
			}

			PartyList.Setup(friends, false, OnPlayerInvited, null, null, null);
		}

		private async void OnPlayerInvited(string id)
		{
			// send invite request
			await Context.Party.Invite(id); // add loading
		}

		private void OnCreateButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			FeatureControl.OpenPartyView();
		}

		private void OnSettingsButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
