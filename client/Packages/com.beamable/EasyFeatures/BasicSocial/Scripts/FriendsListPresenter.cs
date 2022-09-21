using Beamable.Avatars;
using Beamable.Common;
using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class FriendsListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		public FriendSlotPresenter FriendSlotPrefab;
		public PoolableScrollView ScrollView;
		public float DefaultElementHeight = 150;

		protected List<FriendSlotPresenter> SpawnedEntries = new List<FriendSlotPresenter>();
		protected List<FriendSlotPresenter.ViewData> Slots;
		protected Action<long> onButtonPressed;
		protected Action<long> onEntryPressed;
		protected string buttonText;

		public async Promise Setup(List<long> players, Action<long> onEntryPressed = null, Action<long> onButtonPressed = null, string buttonText = "Confirm")
		{
			var Context = BeamContext.Default;
			FriendSlotPresenter.ViewData[] viewData = new FriendSlotPresenter.ViewData[players.Count];
			for (int i = 0; i < players.Count; i++)
			{
				var stats = await Context.Api.Stats.GetStats("client", "public", "player", players[i]);
				if (!stats.TryGetValue("alias", out string playerName))
				{
					playerName = players[i].ToString();
				}

				Sprite avatar = AvatarConfiguration.Instance.Default.Sprite;
				if (stats.TryGetValue("avatar", out string avatarName))
				{
					var accountAvatar = AvatarConfiguration.Instance.Avatars.Find(av => av.Name == avatarName);
					if (accountAvatar != null)
					{
						avatar = accountAvatar.Sprite;
					}
				}

				viewData[i] = new FriendSlotPresenter.ViewData
				{
					PlayerId = players[i],
					PlayerName = playerName,
					Avatar = avatar,
					Description = "Description"
				};
			}

			this.onButtonPressed = onButtonPressed;
			this.buttonText = buttonText;
			this.onEntryPressed = onEntryPressed;
			
			Slots = viewData.ToList();
			ScrollView.SetContentProvider(this);
			
			ClearEntries();
			SpawnEntries();
		}
		
		public void ClearEntries()
		{
			foreach (FriendSlotPresenter slotPresenter in SpawnedEntries)
			{
				Destroy(slotPresenter.gameObject);
			}
			
			SpawnedEntries.Clear();
		}

		public void SpawnEntries()
		{
			var items = new List<PoolableScrollView.IItem>();
			for (int i = 0; i < Slots.Count; i++)
			{
				var data = Slots[i];
				var poolData = new FriendSlotPresenter.PoolData
				{
					ViewData = data, Index = i, Height = DefaultElementHeight
				};
				items.Add(poolData);
			}
			
			ScrollView.SetContent(items);
		}

		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			var spawned = Instantiate(FriendSlotPrefab);
			SpawnedEntries.Add(spawned);
			order = -1;

			var data = item as FriendSlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be FriendSlotPresenter");
			
			spawned.Setup(data, onEntryPressed, onButtonPressed, buttonText);
			
			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform transform)
		{
			if (transform == null) return;
			
			// TODO: implement object pooling
			var slotPresenter = transform.GetComponent<FriendSlotPresenter>();
			SpawnedEntries.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
