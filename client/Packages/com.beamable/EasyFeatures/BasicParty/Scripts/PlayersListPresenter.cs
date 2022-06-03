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
		[SerializeField] private PartySlotPresenter _partySlotPrefab;
		[SerializeField] private PoolableScrollView _scrollView;

		private List<PartySlotPresenter> _spawnedEntries = new List<PartySlotPresenter>();
		private Action<string> _onAcceptButtonClicked;
		private Action<string> _onAskToLeaveClicked;
		private Action<string> _onPromoteClicked;
		private Action _onAddMemberClicked;
		private List<PartySlotPresenter.ViewData> _slots;

		public void Setup(List<PartySlotPresenter.ViewData> slots, Action<string> onPlayerAccepted, Action<string> onAskedToLeave, Action<string> onPromoted, Action onAddMember)
		{
			_slots = slots;
			_scrollView.SetContentProvider(this);
			_onAcceptButtonClicked = onPlayerAccepted;
			_onAskToLeaveClicked = onAskedToLeave;
			_onPromoteClicked = onPromoted;
			_onAddMemberClicked = onAddMember;
			
			ClearEntries();
			SpawnEntries();
		}

		public void ClearEntries()
		{
			foreach (PartySlotPresenter slotPresenter in _spawnedEntries)
			{
				Destroy(slotPresenter.gameObject);
			}
			
			_spawnedEntries.Clear();
		}

		public void SpawnEntries()
		{
			var items = new List<PoolableScrollView.IItem>();
			for (var i = 0; i < _slots.Count; i++)
			{
				var data = _slots[i];
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
			var spawned = Instantiate(_partySlotPrefab);
			_spawnedEntries.Add(spawned);
			order = -1;

			var data = item as PartySlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be PartySlotPresenters");

			spawned.Setup(data.ViewData, _onAcceptButtonClicked, _onAskToLeaveClicked, _onPromoteClicked, _onAddMemberClicked);

			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform rt)
		{
			if (rt == null) return;
			
			// TODO: implement object pooling
			var slotPresenter = rt.GetComponent<PartySlotPresenter>();
			_spawnedEntries.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
