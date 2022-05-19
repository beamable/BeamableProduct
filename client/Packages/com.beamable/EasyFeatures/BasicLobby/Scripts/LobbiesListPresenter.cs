using Beamable.UI.Scripts;
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

		public void Setup(List<LobbiesListEntryPresenter.Data> entries)
		{
			_poolableScrollView.SetContentProvider(this);
			_entriesList = entries;
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
			foreach (var data in _entriesList)
			{
				var rankEntryPoolData = new LobbiesListEntryPresenter.PoolData
				{
					Data = data,
					Height = 100.0f	// TODO: expose this somewhere in inspector
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
			
			spawned.Setup(poolData.Data);
			
			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform rt)
		{
			if (rt == null) return;
			
			// TODO: implement object pooling
			var rankEntryPresenter = rt.GetComponent<LobbiesListEntryPresenter>();
			_spawnedEntries.Remove(rankEntryPresenter);
			Destroy(rankEntryPresenter.gameObject);
		}
	}
}
