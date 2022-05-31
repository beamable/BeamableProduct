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
		private Action<string> _onAskToLeaveClicked;
		private Action<string> _onPromoteClicked;

		public void Setup(Action<string> onPlayerAccepted, Action<string> onAskedToLeave, Action<string> onPromoted)
		{
			_scrollView.SetContentProvider(this);
			_onAcceptButtonClicked = onPlayerAccepted;
			_onAskToLeaveClicked = onAskedToLeave;
			_onPromoteClicked = onPromoted;
		}

		public RectTransform Spawn(PoolableScrollView.IItem item, out int order)
		{
			// TODO: implement object pooling
			var spawned = Instantiate(_partySlotPrefab);
			_spawnedEntries.Add(spawned);
			order = -1;

			var data = item as PartySlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be PartySlotPresenters");

			spawned.Setup(data.Avatar, data.Name, _onAcceptButtonClicked, _onAskToLeaveClicked, _onPromoteClicked);

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
