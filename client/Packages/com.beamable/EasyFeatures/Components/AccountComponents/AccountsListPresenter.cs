using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AccountsListPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
	{
		public AccountSlotPresenter AccountSlotPrefab;
		public PoolableScrollView ScrollView;
		public float DefaultElementHeight = 150;

		protected List<AccountSlotPresenter> SpawnedEntries = new List<AccountSlotPresenter>();
		protected List<AccountSlotPresenter.ViewData> Slots;
		protected Action<long> onButtonPressed;
		protected Action<long> onEntryPressed;
		protected Action<long> onAcceptPressed;
		protected Action<long> onCancelPressed;
		protected Action<long> onDeletePressed;
		protected Action<bool, long> onToggleSwitched;
		protected string buttonText;

		private bool isAcceptCancelVariant;
		private bool isToggleList;
		private ToggleGroup toggleGroup;

		public void Setup(List<AccountSlotPresenter.ViewData> viewData,
		                  Action<long> onButtonPressed = null,
		                  string buttonText = "Confirm",
		                  Action<long> onEntryPressed = null,
		                  Action<long> onDeletePressed = null)
		{
			isAcceptCancelVariant = false;
			isToggleList = false;

			this.onButtonPressed = onButtonPressed;
			this.buttonText = buttonText;
			this.onEntryPressed = onEntryPressed;
			this.onDeletePressed = onDeletePressed;

			SetupInternal(viewData);
		}

		public void Setup(List<AccountSlotPresenter.ViewData> viewData,
		                  Action<long> onAcceptPressed,
		                  Action<long> onCancelPressed,
		                  Action<long> onEntryPressed = null,
		                  Action<long> onDeletePressed = null)
		{
			isAcceptCancelVariant = true;
			isToggleList = false;

			this.onAcceptPressed = onAcceptPressed;
			this.onCancelPressed = onCancelPressed;
			this.onEntryPressed = onEntryPressed;
			this.onDeletePressed = onDeletePressed;

			SetupInternal(viewData);
		}

		public void SetupToggles(List<AccountSlotPresenter.ViewData> viewData,
		                         ToggleGroup group,
		                         Action<bool, long> onToggleSwitched = null,
		                         Action<long> onDeletePressed = null)
		{
			isAcceptCancelVariant = false;
			isToggleList = true;
			toggleGroup = group;
			this.onDeletePressed = onDeletePressed;

			this.onToggleSwitched = onToggleSwitched;

			SetupInternal(viewData);
		}

		private void SetupInternal(List<AccountSlotPresenter.ViewData> viewData)
		{
			Slots = viewData.ToList();
			ScrollView.SetContentProvider(this);

			ClearEntries();
			SpawnEntries();
		}

		public void ClearEntries()
		{
			foreach (AccountSlotPresenter slotPresenter in SpawnedEntries)
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
				var poolData = new AccountSlotPresenter.PoolData
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
			var spawned = Instantiate(AccountSlotPrefab);
			SpawnedEntries.Add(spawned);
			order = -1;

			var data = item as AccountSlotPresenter.PoolData;
			Assert.IsTrue(data != null, "All items in this scroll view MUST be FriendSlotPresenter");

			if (isToggleList)
			{
				spawned.SetupAsToggle(data, toggleGroup, onToggleSwitched, onDeletePressed);
			}
			else if (isAcceptCancelVariant)
			{
				spawned.Setup(data, onEntryPressed, onCancelPressed, onAcceptPressed, onDeletePressed);
			}
			else
			{
				spawned.Setup(data, onEntryPressed, onButtonPressed, buttonText, onDeletePressed);
			}

			return spawned.GetComponent<RectTransform>();
		}

		public void Despawn(PoolableScrollView.IItem item, RectTransform transform)
		{
			if (transform == null) return;

			// TODO: implement object pooling
			var slotPresenter = transform.GetComponent<AccountSlotPresenter>();
			SpawnedEntries.Remove(slotPresenter);
			Destroy(slotPresenter.gameObject);
		}
	}
}
