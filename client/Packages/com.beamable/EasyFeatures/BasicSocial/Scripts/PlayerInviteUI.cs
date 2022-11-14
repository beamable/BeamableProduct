using Beamable.Api;
using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class PlayerInviteUI : MonoBehaviour
	{
		public TMP_InputField PlayerIdInputField;
		public GameObject FoundPlayerObject;
		public FriendSlotPresenter FriendPresenter;

		private BeamContext Context;

		public void Setup(BeamContext context)
		{
			FoundPlayerObject.SetActive(false);

			Context = context;
			
			PlayerIdInputField.onEndEdit.ReplaceOrAddListener(SearchForPlayer);
		}

		private void SearchForPlayer(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				return;
			}
			
			// check if player exists (Presence SDK?)
			// if exists then setup FriendPresenter with this player's data and invite button
		}

		private async void SendInvite(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				return;
			}

			try
			{
				await Context.Social.Invite(id);
				// FeatureControl.OverlaysController.ShowInform($"Sent invite to player {playerId}", null);
			}
			catch (PlatformRequesterException e)
			{
				if (e.Error.status == 404)
				{
					// FeatureControl.OverlaysController.ShowError($"No player found with id {playerId}");
				}
				else
				{
					throw;
				}
			}
		}
	}
}
