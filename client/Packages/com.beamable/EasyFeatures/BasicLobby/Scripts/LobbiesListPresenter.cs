using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbiesListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		[SerializeField] private LobbiesListEntryPresenter _lobbyEntryPrefab;
		[SerializeField] private GameObjectToggler _loadingIndicator;
		[SerializeField] private PoolableScrollView _poolableScrollView;
		
		private readonly List<LobbiesListEntryPresenter> _spawnedEntries = new List<LobbiesListEntryPresenter>();
		private List<LobbiesListEntryPresenter.Data> _entriesList = new List<LobbiesListEntryPresenter.Data>();
		private Action<int> _onLobbySelected;

		private LobbiesListEntryPresenter _currentlySelectedLobby;

		public void Setup(List<LobbiesListEntryPresenter.Data> entries, Action<int> onLobbySelected)
		{
			_poolableScrollView.SetContentProvider(this);
			_entriesList = entries;
			_onLobbySelected = onLobbySelected;
		}

		public void ClearPooledRankedEntries()
		{
			_loadingIndicator.Toggle(true);

			foreach (LobbiesListEntryPresenter entryPresenter in _spawnedEntries)
			{
				Destroy(entryPresenter.gameObject);
			}

			_spawnedEntries.Clear();
		}

		public void RebuildPooledLobbiesEntries()
		{
			var items = new List<PoolableScrollView.IItem>();
			for (var i = 0; i < _entriesList.Count; i++)
			{
				var data = _entriesList[i];
				var rankEntryPoolData = new LobbiesListEntryPresenter.PoolData
				{
					Data = data, Index = i, Height = 100.0f // TODO: expose this somewhere in inspector
				};
				items.Add(rankEntryPoolData);
			}

			_poolableScrollView.SetContent(items);
			_loadingIndicator.Toggle(false);
		}
		
		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			LobbiesListEntryPresenter spawned = Instantiate(_lobbyEntryPrefab);
			_spawnedEntries.Add(spawned);
			order = -1;
			
			LobbiesListEntryPresenter.PoolData poolData = item as LobbiesListEntryPresenter.PoolData;
			Assert.IsTrue(poolData != null, "All items in this scroll view MUST be LobbiesListEntryPresenter");
			
			spawned.Setup(poolData.Data, (presenter) =>
			{
				if (_currentlySelectedLobby != null)
				{
					_currentlySelectedLobby.SetSelected(false);
				}

				_currentlySelectedLobby = presenter;
				_currentlySelectedLobby.SetSelected(true);
				_onLobbySelected.Invoke(poolData.Index);
			});
			
			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform rt)
		{
			if (rt == null) return;
			
			// TODO: implement object pooling
			LobbiesListEntryPresenter rankEntryPresenter = rt.GetComponent<LobbiesListEntryPresenter>();
			rankEntryPresenter.Despawn();
			_spawnedEntries.Remove(rankEntryPresenter);
			Destroy(rankEntryPresenter.gameObject);
		}
	}
}
