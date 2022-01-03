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
using Beamable.Common.Player;
using Beamable.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Player
{
	[Serializable]
	public class PlayerInventory
	{
		private readonly InventoryService _inventoryApi;
		private readonly IPlatformService _platformService;
		private readonly INotificationService _notificationService;
		private readonly IDependencyProvider _provider;
		private readonly CoreConfiguration _config;
		private readonly IContentApi _contentService;
		private readonly ISdkEventService _sdkEventService;
		private readonly SdkEventConsumer _consumer;

		public PlayerCurrencyGroup Currencies { get; }

		private Dictionary<string, PlayerItemGroup> _items = new Dictionary<string, PlayerItemGroup>();

		public PlayerInventory(
			InventoryService inventoryApi,
			IPlatformService platformService,
			INotificationService notificationService,
			IDependencyProvider provider,
			CoreConfiguration config,
			IContentApi contentService,
			ISdkEventService sdkEventService)
		{
			_inventoryApi = inventoryApi;
			_platformService = platformService;
			_notificationService = notificationService;
			_provider = provider;
			_config = config;
			_contentService = contentService;
			_sdkEventService = sdkEventService;

			Currencies  = new PlayerCurrencyGroup(
				_platformService, _inventoryApi, _notificationService, _sdkEventService, _provider
			);

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

			var json = InventoryUpdateBuilderSerializer.ToJson(builder, transaction);
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
					var allItems = GetItems();
					await allItems.Refresh();
					var nextItemId = allItems.Count == 0 ? 1 : allItems.Max(i => i.ItemId) + 1;

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

		private async Promise HandleEvent(SdkEvent evt)
		{
			switch (evt.Event)
			{
				case "update":
					Debug.Log("Running inventory update");
					var json = evt.Args[0];
					var data = InventoryUpdateBuilderSerializer.FromJson(json);
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

						var _ = _consumer.RunAfterReconnection(new SdkEvent(nameof(PlayerInventory), "write-sync"));
						await Apply(builder);
						break;
					}

					try
					{
						await _inventoryApi.Subscribable.GetCurrent();
						await Currencies.Refresh();
					}
					catch (NoConnectivityException)
					{
						// oh well.
					}

					break;
			}
		}
	}
}
