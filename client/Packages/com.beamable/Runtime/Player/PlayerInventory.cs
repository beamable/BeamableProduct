// unset

using Beamable.Api;
using Beamable.Api.Inventory;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Coroutines;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Player
{
	[Serializable]
	public class SerializedDictionaryStringToPlayerItemGroup : SerializableDictionaryStringToSomething<PlayerItemGroup>
	{

	}

	[Serializable]
	public class PlayerInventory
	{
		private readonly InventoryService _inventoryApi;
		private readonly IPlatformService _platformService;
		private readonly INotificationService _notificationService;
		private readonly CoroutineService _coroutineService;
		private readonly IDependencyProvider _provider;
		private readonly CoreConfiguration _config;
		private readonly IContentApi _contentService;
		private readonly ISdkEventService _sdkEventService;
		private readonly SdkEventConsumer _consumer;

		public PlayerCurrencyGroup Currencies { get; }

		[SerializeField]
		private SerializedDictionaryStringToPlayerItemGroup _items = new SerializedDictionaryStringToPlayerItemGroup();
		[SerializeField]
		private SerializableDictionaryStringToString _itemIdToReqId = new SerializableDictionaryStringToString();
		[SerializeField]
		private long _nextOfflineItemId;
		[SerializeField]
		private InventoryUpdateBuilder _offlineUpdate = new InventoryUpdateBuilder();

		public PlayerInventory(
			InventoryService inventoryApi,
			IPlatformService platformService,
			INotificationService notificationService,
			CoroutineService coroutineService,
			IDependencyProvider provider,
			CoreConfiguration config,
			IContentApi contentService,
			ISdkEventService sdkEventService)
		{
			_inventoryApi = inventoryApi;
			_platformService = platformService;
			_notificationService = notificationService;
			_coroutineService = coroutineService;
			_provider = provider;
			_config = config;
			_contentService = contentService;
			_sdkEventService = sdkEventService;

			Currencies  = new PlayerCurrencyGroup(
				_platformService, _inventoryApi, _notificationService, _sdkEventService, _provider
			);

			// var updateRoutine = _coroutineService.StartNew("playerInvOfflineLoop", Update());
			_consumer = _sdkEventService.Register(nameof(PlayerInventory), HandleEvent);
		}

		public PlayerCurrency GetCurrency(CurrencyRef currencyRef) => Currencies.GetCurrency(currencyRef);

		public PlayerItemGroup GetItems(ItemRef itemRef=null)
		{
			itemRef = itemRef ?? "items";

			if (_items.TryGetValue(itemRef, out var group)) return group;

			var itemGroup = new PlayerItemGroup(itemRef, _platformService, _inventoryApi, _provider);
			_items.Add(itemRef, itemGroup);
			return itemGroup;
		}

		public Promise Update(Action<InventoryUpdateBuilder> updateBuilder, string transaction=null)
		{
			var builder = new InventoryUpdateBuilder();
			updateBuilder?.Invoke(builder);

			// serialize the builder, and commit it the log state.
			return Update(builder, transaction);
		}

		public Promise Update(InventoryUpdateBuilder updateBuilder, string transaction = null)
		{
			var json = InventoryUpdateBuilderSerializer.ToNetworkJson(updateBuilder, transaction);
			return _sdkEventService.Add(new SdkEvent(nameof(PlayerInventory), "update", json));
		}

		private async Promise Apply(InventoryUpdateBuilder builder)
		{
			// TODO apply vip bonus
			if (builder.applyVipBonus == true)
			{
				throw new NotImplementedException("Cannot perform vipBonus in offline mode");
			}

			// TODO: apply currency properties.
			if (builder.currencyProperties.Count > 0)
			{
				throw new NotImplementedException("Cannot perform currency properties in offline mode");
			}

			if (builder.newItems.Count > 0)
			{
				// get the fake item group.
				foreach (var newItem in builder.newItems)
				{

					var content = await _contentService.GetContent<ItemContent>(new ItemRef(newItem.contentId));
					if (!content.clientPermission.writeSelf)
						throw new PlatformRequesterException(new PlatformError
						{
							status = 403,
							service = "inventory",
							error = "not authorized",
							message = "in an offline mode, you tried to write to a protected inventory item. That can't be simulated."
						}, null, "403 offline");


					var itemGroup = GetItems(newItem.contentId);

					_nextOfflineItemId --;
					var nextItemId = _nextOfflineItemId;
					_itemIdToReqId[OfflineIdKey(newItem.contentId, nextItemId)] = newItem.requestId;

					if (!_inventoryApi.Subscribable.GetCurrentView().items
					                  .TryGetValue(newItem.contentId, out var existingItems))
					{
						existingItems = new List<ItemView>();
						_inventoryApi.Subscribable.GetCurrentView().items[newItem.contentId] = existingItems;
					}

					existingItems.Add(new ItemView
					{
						createdAt = 0,
						id = nextItemId,
						properties = newItem.properties,
						updatedAt = 0 // TODO: how to get server time in offline way?
					});
					await itemGroup.Refresh(); // TODO: refactor to debounce these calls?
				}
			}

			if (builder.updateItems.Count > 0)
			{

				foreach (var updateItem in builder.updateItems)
				{
					if (!_inventoryApi.Subscribable.GetCurrentView().items
					                  .TryGetValue(updateItem.contentId, out var existingItems))
					{
						existingItems = new List<ItemView>();
						_inventoryApi.Subscribable.GetCurrentView().items[updateItem.contentId] = existingItems;
					}

					var itemGroup = GetItems(updateItem.contentId);
					var existingItem = existingItems.FirstOrDefault(x => x.id == updateItem.itemId);
					if (existingItem == null)
						throw new InvalidOperationException($"Cannot update non existent item while in offline mode. contentid=[{updateItem.contentId}] itemid=[{updateItem.itemId}]");

					existingItem.properties = updateItem.properties;
					existingItem.updatedAt = 0; // TODO how to get server time in offline way?

					await itemGroup.Refresh();
				}
			}

			if (builder.deleteItems.Count > 0)
			{
				foreach (var deleteItem in builder.updateItems)
				{
					if (deleteItem.itemId < 0)
						throw new InvalidOperationException(
							"Cannot delete an item that was created while in offline mode. This is because the id of the item isn't actually known, so the update");
				}
				foreach (var deleteItem in builder.deleteItems)
				{
					if (!_inventoryApi.Subscribable.GetCurrentView().items
					                  .TryGetValue(deleteItem.contentId, out var existingItems))
					{
						existingItems = new List<ItemView>();
						_inventoryApi.Subscribable.GetCurrentView().items[deleteItem.contentId] = existingItems;
					}

					var itemGroup = GetItems(deleteItem.contentId);
					var existingItem = existingItems.FirstOrDefault(x => x.id == deleteItem.itemId);
					if (existingItem == null)
						throw new InvalidOperationException($"Cannot delete non existent item while in offline mode. contentid=[{deleteItem.contentId}] itemid=[{deleteItem.itemId}]");

					existingItems.Remove(existingItem);
					await itemGroup.Refresh();

				}
			}

			foreach (var kvp in builder.currencies)
			{
				Currencies.GetCurrency(kvp.Key).Amount += kvp.Value;
			}

		}


		private string OfflineIdKey(string contentId, long itemId) => $"{contentId}-{itemId}";

		private bool TryGetOfflineId(string contentId, long itemId, out string reqId)
		{
			return _itemIdToReqId.TryGetValue(OfflineIdKey(contentId, itemId), out reqId);
		}

		private void UpdateOfflineBuilder(InventoryUpdateBuilder builder){
			// TODO: merge currencies and vip_bonus and such.

			foreach (var curr in builder.currencies)
			{
				_offlineUpdate.CurrencyChange(curr.Key, curr.Value);
			}

			_offlineUpdate.newItems.AddRange(builder.newItems);
			_offlineUpdate.updateItems.AddRange(builder.updateItems);
			_offlineUpdate.deleteItems.AddRange(builder.deleteItems);

			// if we delete an item that we had only ever added offline, then we don't ever send it. So we remove it from the newItems
			foreach (var delete in builder.deleteItems)
			{
				if (TryGetOfflineId(delete.contentId, delete.itemId, out var reqId))
				{
					var newItem = _offlineUpdate.newItems.FirstOrDefault(item => item.requestId == reqId);
					var deleteItem = _offlineUpdate.deleteItems.FirstOrDefault(
						item => item.contentId == delete.contentId && item.itemId == delete.itemId);
					_offlineUpdate.newItems.Remove(newItem);
					_offlineUpdate.deleteItems.Remove(deleteItem);
				}
			}

			// if we update an item that we only ever added offline, then we don't send the update request, but instead modify the original start parameters...
			foreach (var update in builder.updateItems)
			{
				if (TryGetOfflineId(update.contentId, update.itemId, out var reqId))
				{
					var newItem = _offlineUpdate.newItems.FirstOrDefault(item => item.requestId == reqId);
					if (newItem == null)
						throw new InvalidOperationException($"Cannot update item that doesnt exist in builder. {update.contentId}-{update.itemId}");
					newItem.properties = update.properties;
					var updateItem = _offlineUpdate.updateItems.FirstOrDefault(
						item => item.contentId == update.contentId && item.itemId == update.itemId);
					_offlineUpdate.updateItems.Remove(updateItem);
				}
			}
		}

		private async Promise HandleUpdate(SdkEvent evt)
		{
			var json = evt.Args[0];
			var data = InventoryUpdateBuilderSerializer.FromNetworkJson(json);
			var builder = data.Item1;
			var transaction = data.Item2;
			try
			{
				await _inventoryApi.Update(builder, transaction);
			}
			catch (NoConnectivityException)
			{
				if (_config.InventoryOfflineMode == CoreConfiguration.OfflineStrategy.Disable)
				{
					throw;
				}

				Debug.Log("Adding resync later");
				UpdateOfflineBuilder(builder);
				var _ = _consumer.RunAfterReconnection(new SdkEvent(nameof(PlayerInventory), "commit", json));
				await Apply(builder);
				return;
			}

			try
			{
				// await _inventoryApi.Subscribable.GetCurrent();

				foreach (var itemGroup in _items.Values)
				{
					await itemGroup.Refresh();
				}
				await Currencies.Refresh();
			}
			catch (NoConnectivityException)
			{
				// oh well.
			}
		}


		private async Promise HandleEvent(SdkEvent evt)
		{
			switch (evt.Event)
			{
				case "update":
					Debug.Log("Ran update!");
					await HandleUpdate(evt);
					break;

				case "commit":
					if (_offlineUpdate.IsEmpty)
					{
						Debug.Log("offline builder is empty, skipping commit");
						break;
					}
					Debug.Log("Ran commit!");

					var nextBuilder = _offlineUpdate;
					_offlineUpdate = new InventoryUpdateBuilder();
					await Update(nextBuilder);
					_itemIdToReqId.Clear();
					break;
			}
		}
	}
}
