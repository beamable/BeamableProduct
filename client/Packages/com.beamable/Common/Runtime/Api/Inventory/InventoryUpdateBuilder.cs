using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Common.Inventory;

namespace Beamable.Common.Api.Inventory
{
	/// <summary>
	/// This type defines the %Inventory feature's create request.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.Api.Inventory.InventoryService script reference 
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[Serializable]
	public class ItemCreateRequest
	{
		public string contentId;
		public Dictionary<string, string> properties;
	}

	/// <summary>
	/// This type defines the %Inventory feature's delete request.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.Api.Inventory.InventoryService script reference 
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[Serializable]
	public class ItemDeleteRequest
	{
		public string contentId;
		public long itemId;
	}

	/// <summary>
	/// This type defines the %Inventory feature's update request.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.Api.Inventory.InventoryService script reference 
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class ItemUpdateRequest
	{
		public string contentId;
		public long itemId;
		public Dictionary<string, string> properties;
	}

	/// <summary>
	/// This type defines the %Inventory feature's updates.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.Api.Inventory.InventoryService script reference 
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class InventoryUpdateBuilder
	{
		public readonly Dictionary<string, long> currencies;
		public readonly Dictionary<string, List<CurrencyProperty>> currencyProperties;
		public readonly List<ItemCreateRequest> newItems;
		public readonly List<ItemDeleteRequest> deleteItems;
		public readonly List<ItemUpdateRequest> updateItems;
		public bool? applyVipBonus;

		public bool IsEmpty
		{
			get
			{
				return currencies.Count == 0 &&
				       currencyProperties.Count == 0 &&
				       newItems.Count == 0 &&
				       deleteItems.Count == 0 &&
				       updateItems.Count == 0;
			}
		}

		public InventoryUpdateBuilder()
		{
			currencies = new Dictionary<string, long>();
			currencyProperties = new Dictionary<string, List<CurrencyProperty>>();
			newItems = new List<ItemCreateRequest>();
			deleteItems = new List<ItemDeleteRequest>();
			updateItems = new List<ItemUpdateRequest>();
		}

		public InventoryUpdateBuilder ApplyVipBonus(bool apply)
		{
			applyVipBonus = apply;

			return this;
		}

		public InventoryUpdateBuilder CurrencyChange(string contentId, long amount)
		{
			if (currencies.TryGetValue(contentId, out var currentValue))
			{
				currencies[contentId] = currentValue + amount;
			}
			else
			{
				currencies.Add(contentId, amount);
			}

			return this;
		}

		public InventoryUpdateBuilder SetCurrencyProperties(string contentId, List<CurrencyProperty> properties)
		{
			currencyProperties[contentId] = properties;

			return this;
		}

		public InventoryUpdateBuilder AddItem(string contentId, Dictionary<string, string> properties = null)
		{
			newItems.Add(new ItemCreateRequest
			{
				contentId = contentId, properties = properties ?? new Dictionary<string, string>()
			});

			return this;
		}

		public InventoryUpdateBuilder AddItem(ItemRef itemRef, Dictionary<string, string> properties = null) =>
			AddItem(itemRef.Id, properties);

		public InventoryUpdateBuilder DeleteItem(string contentId, long itemId)
		{
			deleteItems.Add(new ItemDeleteRequest {contentId = contentId, itemId = itemId});

			return this;
		}

		public InventoryUpdateBuilder DeleteItem<TContent>(long itemId) where TContent : ItemContent, new()
		{
			var contentId = ContentRegistry.TypeToName(typeof(TContent));
			return DeleteItem(contentId, itemId);
		}

		public InventoryUpdateBuilder DeleteItem<TContent>(InventoryObject<TContent> item)
			where TContent : ItemContent, new()
		{
			return DeleteItem(item.ItemContent.Id, item.Id);
		}

		public InventoryUpdateBuilder UpdateItem(string contentId, long itemId, Dictionary<string, string> properties)
		{
			updateItems.Add(new ItemUpdateRequest {contentId = contentId, itemId = itemId, properties = properties});

			return this;
		}

		public InventoryUpdateBuilder UpdateItem<TContent>(long itemId, Dictionary<string, string> properties)
			where TContent : ItemContent, new()
		{
			var contentId = ContentRegistry.TypeToName(typeof(TContent));
			return UpdateItem(contentId, itemId, properties);
		}

		public InventoryUpdateBuilder UpdateItem<TContent>(InventoryObject<TContent> item)
			where TContent : ItemContent, new()
		{
			return UpdateItem(item.ItemContent.Id, item.Id, item.Properties);
		}
	}
}
