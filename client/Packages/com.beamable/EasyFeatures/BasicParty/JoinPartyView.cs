using Beamable.UI.Buss;
using EasyFeatures.Components;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public struct PartyInvite
	{
		public string partyId, playerId;
	}
	
	public class JoinPartyView : MonoBehaviour, ISyncBeamableView
	{
		// Use invites list stored on backend once ready
		public static List<PartyInvite> ReceivedInvites = new List<PartyInvite>();
		[SerializeField] private GameObject _noInvitesPendingText;
		
		public PartyFeatureControl FeatureControl;
		public int EnrichOrder;

		public PlayersListPresenter InvitesList;
		public Button BackButton;

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

			_noInvitesPendingText.SetActive(ReceivedInvites.Count == 0);

			List<long> playerIds = new List<long>(ReceivedInvites.Count);
			foreach (var invite in ReceivedInvites)
			{
				if (long.TryParse(invite.playerId, out long id))
				{
					playerIds.Add(id);
				}
			}

			await InvitesList.Setup(playerIds, false, OnInviteAccepted, null, null, null);
			
			BackButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private async void OnInviteAccepted(string playerId)
		{
			for (int i = 0; i < ReceivedInvites.Count; i++)
			{
				var invite = ReceivedInvites[i];
				if (invite.playerId == playerId)
				{
					ReceivedInvites.Remove(invite);
					await Context.Party.Join(invite.partyId);
					FeatureControl.OpenPartyView();
					return;
				}
			}
		}

		private void OnBackButtonClicked()
		{
			FeatureControl.OpenCreatePartyView();
		}
	}
}
