using Beamable.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicFriendsView : MonoBehaviour, IAsyncBeamableView
	{
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int EnrichOrder = 0;

		public FriendsListPresenter FriendsListPresenter;
		
		public int GetEnrichOrder() => EnrichOrder;

		protected BeamContext Context;

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();

			if (!IsVisible)
			{
				return;
			}

			await Context.Social.OnReady;

			List<long> friends = new List<long>(Context.Social.Friends.Count);
			foreach (var friend in Context.Social.Friends)
			{
				friends.Add(friend.playerId);
			}

			await FriendsListPresenter.Setup(friends);
		}
	}
}
