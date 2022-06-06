using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PlayersListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		public PoolableScrollView PoolableScrollView;
		public PartySlotPresenter PartySlotPrefab;
		public PoolableScrollView ScrollView;

		protected List<PartySlotPresenter> SpawnedEntries = new List<PartySlotPresenter>();
		protected Action<string> OnAcceptButtonClicked;
		protected Action<string> OnAskToLeaveClicked;
		protected Action<string> OnPromoteClicked;
		protected Action OnAddMemberClicked;
		protected List<PartySlotPresenter.ViewData> Slots;

		public void Setup(List<PartySlotPresenter.ViewData> slots, Action<string> onPlayerAccepted, Action<string> onAskedToLeave, Action<string> onPromoted, Action onAddMember)
		{
			Slots = slots;
			ScrollView.SetContentProvider(this);
			OnAcceptButtonClicked = onPlayerAccepted;
			OnAskToLeaveClicked = onAskedToLeave;
			OnPromoteClicked = onPromoted;
			OnAddMemberClicked = onAddMember;
			
			ClearEntries();
			SpawnEntries();
		}

		public void ClearEntries()
		{
			foreach (PartySlotPresenter slotPresenter in SpawnedEntries)
			{
				Destroy(slotPresenter.gameObject);
			}
			
			SpawnedEntries.Clear();
		}

		public void SpawnEntries()
		{
			var items = new List<PoolableScrollView.IItem>();
			for (var i = 0; i < Slots.Count; i++)
			{
				var data = Slots[i];
				var rankEntryPoolData = new PartySlotPresenter.PoolData
				{
					ViewData = data, Index = i, Height = 150.0f // TODO: expose this somewhere in inspector
				};
				items.Add(rankEntryPoolData);
			}

			PoolableScrollView.SetContent(items);
		}

		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			var spawned = Instantiate(PartySlotPrefab);
			SpawnedEntries.Add(spawned);
			order = -1;

			var data = item as PartySlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be PartySlotPresenters");

			spawned.Setup(data.ViewData, OnAcceptButtonClicked, OnAskToLeaveClicked, OnPromoteClicked, OnAddMemberClicked);

			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform rt)
		{
			if (rt == null) return;
			
			// TODO: implement object pooling
			var slotPresenter = rt.GetComponent<PartySlotPresenter>();
			SpawnedEntries.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
