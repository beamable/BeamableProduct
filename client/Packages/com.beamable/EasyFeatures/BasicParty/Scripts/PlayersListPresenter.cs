using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PlayersListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		[SerializeField] private PartySlotPresenter _partySlotPrefab;
		[SerializeField] private PoolableScrollView _scrollView;

		private List<PartySlotPresenter> _spawnedEntries = new List<PartySlotPresenter>();
		private Action<string> _onAcceptButtonClicked;
		
		public void Setup(Action<string> onPlayerAccepted)
		{
			_scrollView.SetContentProvider(this);
			_onAcceptButtonClicked -= onPlayerAccepted;
			_onAcceptButtonClicked += onPlayerAccepted;
		}
		
		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			var spawned = Instantiate(_partySlotPrefab);
			_spawnedEntries.Add(spawned);
			order = -1;

			var data = item as PartySlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be PartySlotPresenters");
			
			spawned.Setup(data.Avatar, data.Name, _onAcceptButtonClicked, null, null);

			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform transform)
		{
			throw new System.NotImplementedException();
		}
	}
}
