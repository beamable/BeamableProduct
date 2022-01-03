using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;
using System.Linq;
using UnityEngine;

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
    [Serializable]
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

        public InventoryUpdateBuilder(
	        Dictionary<string, long> currencies,
			Dictionary<string, List<CurrencyProperty>> currencyProperties,
	        List<ItemCreateRequest> newItems,
	        List<ItemDeleteRequest> deleteItems,
	        List<ItemUpdateRequest> updateItems
	        ) : this()
        {
	        this.currencies = currencies;
	        this.currencyProperties = currencyProperties;
	        this.newItems = newItems;
	        this.deleteItems = deleteItems;
	        this.updateItems = updateItems;
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

        public InventoryUpdateBuilder AddItem(string contentId, Dictionary<string, string> properties=null)
        {
            newItems.Add(new ItemCreateRequest
            {
                contentId = contentId,
                properties = properties ?? new Dictionary<string, string>()
            });

            return this;
        }

        public InventoryUpdateBuilder AddItem(ItemRef itemRef, Dictionary<string, string> properties = null)
            => AddItem(itemRef.Id, properties);


        public InventoryUpdateBuilder DeleteItem(string contentId, long itemId)
        {
            deleteItems.Add(new ItemDeleteRequest
            {
                contentId = contentId,
                itemId = itemId
            });

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
            updateItems.Add(new ItemUpdateRequest
            {
                contentId = contentId,
                itemId = itemId,
                properties = properties
            });

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

    public static class InventoryUpdateBuilderSerializer
    {
	    private const string TRANSACTION = "transaction";
	    private const string APPLY_VIP_BONUS = "applyVipBonus";
	    private const string CURRENCIES = "currencies";
	    private const string CURRENCY_PROPERTIES = "currencyProperties";
	    private const string CURRENCY_PROPERTY_NAME = "name";
	    private const string CURRENCY_PROPERTY_VALUE = "value";
	    private const string NEW_ITEMS = "newItems";
	    private const string NEW_ITEM_CONTENT_ID = "contentId";
	    private const string NEW_ITEM_PROPERTIES = "properties";
	    private const string NEW_ITEM_PROPERTY_NAME = "name";
	    private const string NEW_ITEM_PROPERTY_VALUE = "value";
	    private const string DELETE_ITEMS = "deleteItems";
	    private const string DELETE_ITEMS_CONTENT_ID = "contentId";
	    private const string DELETE_ITEMS_ITEM_ID = "id";
	    private const string UPDATE_ITEMS = "updateItems";
	    private const string UPDATE_ITEMS_ITEM_ID = "id";
	    private const string UPDATE_ITEMS_CONTENT_ID = "contentId";
	    private const string UPDATE_ITEMS_PROPERTIES = "properties";


	    public static (InventoryUpdateBuilder, string) FromJson(string json)
	    {
		    string transaction = null;
		    bool? applyVipBonus = null;
		    var currencies = new Dictionary<string, long>();
		    var currencyProperties = new Dictionary<string, List<CurrencyProperty>>();
		    var newItems = new List<ItemCreateRequest>();
		    var deleteItems = new List<ItemDeleteRequest>();
		    var updateItems = new List<ItemUpdateRequest>();

		    var dict = Json.Deserialize(json) as ArrayDict;
		    if (dict.TryGetValue(TRANSACTION, out var transactionObj) && transactionObj is string storedTransaction)
		    {
			    transaction = storedTransaction;
		    }

		    if (dict.TryGetValue(APPLY_VIP_BONUS, out var applyVipBonusObj) && applyVipBonusObj is bool storedApplyVipBonus)
		    {
			    applyVipBonus = storedApplyVipBonus;
		    }

		    if (dict.TryGetValue(CURRENCIES, out var currencyObj) && currencyObj is ArrayDict storedCurrencies)
		    {
			    currencies = storedCurrencies.ToDictionary(kvp => kvp.Key, kvp => (long)kvp.Value);;
		    }

		    if (dict.TryGetValue(CURRENCY_PROPERTIES, out var currPropsObj) && currPropsObj is ArrayDict storedCurrProps)
		    {
			    currencyProperties = storedCurrProps.ToDictionary(
				    kvp => kvp.Key,
				    kvp =>
				    {
					    List<CurrencyProperty> props = null;
					    if (kvp.Value is List<object> objs)
					    {
						    var subDict = objs.Cast<ArrayDict>();
						    props = subDict.Select(x => new CurrencyProperty
						    {
							    name = x[CURRENCY_PROPERTY_NAME]?.ToString(),
							    value = x[CURRENCY_PROPERTY_VALUE]?.ToString(),
						    }).ToList();
					    }
					    return props ?? new List<CurrencyProperty>();
				    });
		    }

		    if (dict.TryGetValue(NEW_ITEMS, out var newItemsObjs) && newItemsObjs is List<object> newItemsObjList)
		    {
			    var subDicts = newItemsObjList.Cast<ArrayDict>().ToList();
			    newItems = subDicts.Select(x =>
			    {
				    var propsObjs = ((List<object>)x[NEW_ITEM_PROPERTIES]).Cast<ArrayDict>().ToList();
				    var propsDict = propsObjs.ToDictionary(p => p[NEW_ITEM_PROPERTY_NAME]?.ToString(),
				                                           p => p[NEW_ITEM_PROPERTY_VALUE]?.ToString());
				    return new ItemCreateRequest
				    {
					    contentId = x[NEW_ITEM_CONTENT_ID]?.ToString(),
					    properties = propsDict
				    };
			    }).ToList();
		    }

		    if (dict.TryGetValue(DELETE_ITEMS, out var deleteItemsObj) && deleteItemsObj is List<object> deleteItemsObjList)
		    {
			    var subDicts = deleteItemsObjList.Cast<ArrayDict>().ToList();
			    deleteItems = subDicts.Select(x =>
			    {
				    return new ItemDeleteRequest()
				    {
					    contentId = x[DELETE_ITEMS_CONTENT_ID]?.ToString(),
					    itemId = long.Parse(x[DELETE_ITEMS_ITEM_ID]?.ToString())
				    };
			    }).ToList();
		    }

		    if (dict.TryGetValue(UPDATE_ITEMS, out var updateItemsObj) && updateItemsObj is List<object> updateItemsObjList)
		    {
			    var subDicts = updateItemsObjList.Cast<ArrayDict>().ToList();
			    updateItems = subDicts.Select(x =>
			    {
				    var propsObjs = ((List<object>)x[UPDATE_ITEMS_PROPERTIES]).Cast<ArrayDict>().ToList();
				    var propsDict = propsObjs.ToDictionary(p => p[NEW_ITEM_PROPERTY_NAME]?.ToString(),
				                                           p => p[NEW_ITEM_PROPERTY_VALUE]?.ToString());
				    return new ItemUpdateRequest()
				    {
					    contentId = x[UPDATE_ITEMS_CONTENT_ID]?.ToString(),
					    itemId = long.Parse(x[UPDATE_ITEMS_ITEM_ID]?.ToString()),
					    properties = propsDict
				    };
			    }).ToList();
		    }

		    var builder = new InventoryUpdateBuilder(currencies, currencyProperties, newItems, deleteItems, updateItems);
		    builder.applyVipBonus = applyVipBonus;

		    return (builder, transaction);
	    }

	    public static string ToJson(InventoryUpdateBuilder builder, string transaction=null)
	    {
		    using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
		    {
			    var dict = new ArrayDict();
			    if (!string.IsNullOrEmpty(transaction))
			    {
				    dict.Add(TRANSACTION, transaction);
			    }

			    if (builder.applyVipBonus.HasValue)
			    {
				    dict.Add(APPLY_VIP_BONUS, builder.applyVipBonus.Value);
			    }

			    if (builder.currencies != null && builder.currencies.Count > 0)
			    {
				    dict.Add(CURRENCIES, builder.currencies);
			    }

			    if (builder.currencyProperties != null && builder.currencyProperties.Count > 0)
			    {
				    var currencyDict = new ArrayDict();
				    foreach (var kvp in builder.currencyProperties)
				    {
					    var newProperties = kvp.Value.Select(newProperty => new ArrayDict
					    {
						    {CURRENCY_PROPERTY_NAME, newProperty.name},
						    {CURRENCY_PROPERTY_VALUE, newProperty.value}
					    }).ToArray();
					    currencyDict.Add(kvp.Key, newProperties);
				    }

				    dict.Add(CURRENCY_PROPERTIES, currencyDict);
			    }

			    if (builder.newItems != null && builder.newItems.Count > 0)
			    {
				    var newItems = builder.newItems.Select(newItem => new ArrayDict
				    {
					    {NEW_ITEM_CONTENT_ID, newItem.contentId},
					    {
						    NEW_ITEM_PROPERTIES, newItem.properties
						                                ?.Select(
							                                kvp => new ArrayDict {{NEW_ITEM_PROPERTY_NAME, kvp.Key}, {NEW_ITEM_PROPERTY_VALUE, kvp.Value}})
						                                .ToArray() ?? new object[] { }
					    }
				    }).ToArray();

				    dict.Add(NEW_ITEMS, newItems);
			    }

			    if (builder.deleteItems != null && builder.deleteItems.Count > 0)
			    {
				    var deleteItems = builder.deleteItems.Select(deleteItem => new ArrayDict
				    {
					    {DELETE_ITEMS_CONTENT_ID, deleteItem.contentId},
					    {DELETE_ITEMS_ITEM_ID, deleteItem.itemId}
				    }).ToArray();

				    dict.Add(DELETE_ITEMS, deleteItems);
			    }

			    if (builder.updateItems != null && builder.updateItems.Count > 0)
			    {
				    var updateItems = builder.updateItems.Select(updateItem => new ArrayDict
				    {
					    {"contentId", updateItem.contentId},
					    {"id", updateItem.itemId},
					    {
						    "properties", updateItem.properties
						                            .Select(kvp => new ArrayDict
						                            {
							                            {"name", kvp.Key}, {"value", kvp.Value}
						                            }).ToArray()
					    }
				    }).ToArray();

				    dict.Add(UPDATE_ITEMS, updateItems);
			    }

			    var json = Json.Serialize(dict, pooledBuilder.Builder);
			    return json;
		    }
	    }
    }
}
