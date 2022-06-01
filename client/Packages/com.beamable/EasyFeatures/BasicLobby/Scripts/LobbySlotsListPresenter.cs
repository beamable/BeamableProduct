using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbySlotsListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		[Header("Prefabs")]
		public LobbySlotPresenter LobbySlotPrefab;

		[Header("Components")]
		public GameObjectToggler LoadingIndicator;
		public PoolableScrollView PoolableScrollView;

		private List<LobbySlotPresenter.ViewData> _slots;
		private bool _isAdmin;
		private Action<int> _onReadyButtonClicked;
		private Action<int> _onNotReadyButtonClicked;
		private Action<int> _onAdminButtonClicked;
		private readonly List<LobbySlotPresenter> _spawnedSlots = new List<LobbySlotPresenter>();

		public void Setup(List<LobbySlotPresenter.ViewData> slots,
		                  bool isAdmin,
		                  Action<int> onReadyButtonClicked,
		                  Action<int> onNotReadyButtonClicked,
		                  Action<int> onAdminButtonClicked)
		{
			PoolableScrollView.SetContentProvider(this);

			_slots = slots;
			_isAdmin = isAdmin;
			_onReadyButtonClicked = onReadyButtonClicked;
			_onNotReadyButtonClicked = onNotReadyButtonClicked;
			_onAdminButtonClicked = onAdminButtonClicked;
		}
		
		public void ClearPooledRankedEntries()
		{
			LoadingIndicator.Toggle(true);

			foreach (LobbySlotPresenter entryPresenter in _spawnedSlots)
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
				var rankEntryPoolData = new LobbySlotPresenter.PoolData
				{
					ViewData = data, Index = i, Height = 150.0f // TODO: expose this somewhere in inspector
				};
				items.Add(rankEntryPoolData);
			}

			PoolableScrollView.SetContent(items);
			LoadingIndicator.Toggle(false);
		}

		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			LobbySlotPresenter spawned = Instantiate(LobbySlotPrefab);
			_spawnedSlots.Add(spawned);
			order = -1;

			LobbySlotPresenter.PoolData poolData = item as LobbySlotPresenter.PoolData;
			Assert.IsTrue(poolData != null, "All items in this scroll view MUST be LobbySlotPresenter");

			if (poolData.ViewData.PlayerId != String.Empty) // Temporarily Name is set to playerId
			{
				spawned.SetupFilled(poolData.ViewData.PlayerId, poolData.ViewData.IsReady, _isAdmin,
				                    () =>
				                    {
					                    _onReadyButtonClicked.Invoke(poolData.Index);
				                    },
				                    () =>
				                    {
					                    _onNotReadyButtonClicked.Invoke(poolData.Index);  
				                    },
				                    () =>
				                    {
					                    _onAdminButtonClicked.Invoke(poolData.Index);
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
			LobbySlotPresenter slotPresenter = rt.GetComponent<LobbySlotPresenter>();
			_spawnedSlots.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
