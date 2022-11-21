using Beamable.Api;
using Beamable.Common;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class PlayerInviteUI : MonoBehaviour
	{
		public TMP_InputField PlayerIdInputField;
		public GameObject FoundPlayerObject;
		public AccountSlotPresenter accountPresenter;

		private BasicInvitesView.IDependencies PlayerSystem;

		public void Setup(BasicInvitesView.IDependencies playerSystem)
		{
			FoundPlayerObject.SetActive(false);

			PlayerSystem = playerSystem;

			PlayerIdInputField.onEndEdit.ReplaceOrAddListener(SearchForPlayer);
			PlayerIdInputField.OnSelect(null);
		}

		private async void SearchForPlayer(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				FoundPlayerObject.SetActive(false);
				return;
			}

			// check if player exists (Presence SDK?)
			// if exists then setup FriendPresenter with this player's data and invite button
			await PlayerFound(id);
		}

		private async Promise PlayerFound(string playerId)
		{
			if (long.TryParse(playerId, out long id))
			{
				await PlayerFound(id);
			}
			else
			{
				Debug.LogError($"'{playerId}' is not a valid player ID");
			}
		}

		private async Promise PlayerFound(long playerId)
		{
			var list = await PlayerSystem.GetPlayersViewData(new List<long> {playerId});
			AccountSlotPresenter.ViewData viewData = list[0];
			AccountSlotPresenter.PoolData data = new AccountSlotPresenter.PoolData {ViewData = viewData};
			accountPresenter.Setup(data, null, SendInvite, "Invite");
			FoundPlayerObject.SetActive(true);
		}

		private async void SendInvite(long playerId)
		{
			try
			{
				await PlayerSystem.Context.Social.Invite(playerId);
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
