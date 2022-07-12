using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class MatchmakingSlotsListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		[Header("Prefabs")]
		public MatchmakingSlotPresenter LobbySlotPrefab;

		[Header("Components")]
		public PoolableScrollView PoolableScrollView;

		private List<MatchmakingSlotPresenter.ViewData> _slots;
		private bool _isAdmin;
		private Action<int> _onAdminButtonClicked;
		private Action<int> _onKickButtonClicked;
		private readonly List<MatchmakingSlotPresenter> _spawnedSlots = new List<MatchmakingSlotPresenter>();

		public void Setup(List<MatchmakingSlotPresenter.ViewData> slots,
		                  bool isAdmin,
		                  Action<int> onAdminButtonClicked,
		                  Action<int> onKickButtonClicked)
		{
			PoolableScrollView.SetContentProvider(this);

			_slots = slots;
			_isAdmin = isAdmin;
			_onAdminButtonClicked = onAdminButtonClicked;
			_onKickButtonClicked = onKickButtonClicked;
		}

		public void ClearPooledRankedEntries()
		{
			foreach (MatchmakingSlotPresenter entryPresenter in _spawnedSlots)
			{
				Destroy(entryPresenter.gameObject);
			}

			_spawnedSlots.Clear();
		}

		public void RebuildPooledLobbiesEntries()
		{
			var items = new List<PoolableScrollView.IItem>();
			for (var i = 0; i < _slots.Count; i++)
			{
				var data = _slots[i];
				MatchmakingSlotPresenter.ViewData rankEntryPoolData = new MatchmakingSlotPresenter.ViewData
				{
					PlayerId = data.PlayerId,
					Team = data.Team,
					IsUnfolded = data.IsUnfolded,
					Index = i,
					Height = data.IsUnfolded ? data.UnfoldedHeight : data.FoldedHeight
				};
				items.Add(rankEntryPoolData);
			}

			PoolableScrollView.SetContent(items);
		}

		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			MatchmakingSlotPresenter spawned = Instantiate(LobbySlotPrefab);
			_spawnedSlots.Add(spawned);
			order = -1;

			MatchmakingSlotPresenter.ViewData poolData = item as MatchmakingSlotPresenter.ViewData;
			Assert.IsTrue(poolData != null, "All items in this scroll view MUST be MatchmakingSlotPresenter");

			if (poolData.PlayerId != String.Empty) // Temporarily Name is set to playerId
			{
				spawned.SetupFilled(poolData.PlayerId, poolData.Team, _isAdmin, poolData.IsUnfolded,
				                    () =>
				                    {
					                    _onAdminButtonClicked.Invoke(poolData.Index);
				                    },
				                    () =>
				                    {
					                    _onKickButtonClicked.Invoke(poolData.Index);
				                    });
			}
			else
			{
				spawned.SetupEmpty();
			}

			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform rt)
		{
			if (rt == null) return;

			// TODO: implement object pooling
			MatchmakingSlotPresenter slotPresenter = rt.GetComponent<MatchmakingSlotPresenter>();
			_spawnedSlots.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
