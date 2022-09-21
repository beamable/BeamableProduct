using Beamable.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicBlockedView : MonoBehaviour, IAsyncBeamableView
	{
		public FriendsListPresenter BlockedListPresenter;

		protected BeamContext Context;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder()
		{
			return 0;
		}

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();

			if (!IsVisible)
			{
				return;
			}

			await Context.Social.OnReady;

			List<long> blockedPlayers = new List<long>(Context.Social.Blocked.Count);
			foreach (var blocked in Context.Social.Blocked)
			{
				blockedPlayers.Add(blocked.playerId);
			}

			await BlockedListPresenter.Setup(blockedPlayers, UnblockPlayer);
		}

		private async void UnblockPlayer(long playerId)
		{
			await Context.Social.UnblockPlayer(playerId);
		}
	}
}
