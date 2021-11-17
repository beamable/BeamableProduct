using Beamable.Common.Api.Content;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Api.Inventory
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Inventory feature.
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
	public abstract class AbsInventoryApi : IInventoryApi
	{
		public const string SERVICE_OBJECT = "object/inventory";

		public IBeamableRequester Requester
		{
			get;
		}

		public IUserContext UserContext
		{
			get;
		}

		public AbsInventoryApi(IBeamableRequester requester, IUserContext userContext)
		{
			Requester = requester;
			UserContext = userContext;
		}

		public Promise<PreviewCurrencyGainResponse> PreviewCurrencyGain(Dictionary<string, long> currencyIdsToAmount)
		{
			using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				var dict = new ArrayDict();
				dict.Add("currencies", currencyIdsToAmount);

				var json = Json.Serialize(dict, pooledBuilder.Builder);
				return Requester.Request<PreviewCurrencyGainResponse>(
					Method.PUT, $"/{SERVICE_OBJECT}/{UserContext.UserId}/preview", json);
			}
		}

		public Promise<GetMultipliersResponse> GetMultipliers()
		{
			return Requester.Request<GetMultipliersResponse>(
				Method.GET, $"/{SERVICE_OBJECT}/{UserContext.UserId}/multipliers");
		}

		public Promise<long> GetCurrency(CurrencyRef currency)
		{
			return GetCurrency(currency.Id);
		}

		public Promise<Unit> AddItem(ItemRef itemRef,
									 Dictionary<string, string> properties = null,
									 string transaction = null) =>
			AddItem(itemRef.Id, properties, transaction);

		public Promise<Unit> AddItem(string contentId,
									 Dictionary<string, string> properties = null,
									 string transaction = null)
		{
			return Update(builder =>
			{
				builder.AddItem(contentId, properties);
			}, transaction);
		}

		public Promise<Unit> DeleteItem(string contentId, long itemId, string transaction = null)
		{
			return Update(builder =>
			{
				builder.DeleteItem(contentId, itemId);
			}, transaction);
		}

		public Promise<Unit> UpdateItem(ItemRef itemRef,
										long itemId,
										Dictionary<string, string> properties,
										string transaction = null) =>
			UpdateItem(itemRef.Id, itemId, properties, transaction);

		public Promise<Unit> UpdateItem(string contentId,
										long itemId,
										Dictionary<string, string> properties,
										string transaction = null)
		{
			return Update(builder =>
			{
				builder.UpdateItem(contentId, itemId, properties);
			}, transaction);
		}

		public Promise<Unit> SetCurrency(CurrencyRef currency, long amount, string transaction = null)
		{
			return SetCurrency(currency.Id, amount, transaction);
		}

		public Promise<Unit> SetCurrencyProperties(string currencyId,
												   List<CurrencyProperty> properties,
												   string transaction = null)
		{
			return Update(builder =>
			{
				builder.SetCurrencyProperties(currencyId, properties);
			}, transaction);
		}

		public Promise<Unit> SetCurrencyProperties(CurrencyRef currency,
												   List<CurrencyProperty> properties,
												   string transaction = null)
		{
			return Update(builder =>
			{
				builder.SetCurrencyProperties(currency.Id, properties);
			}, transaction);
		}

		public Promise<Unit> AddCurrency(string currencyId, long amount, string transaction = null)
		{
			return Update(builder =>
			{
				builder.CurrencyChange(currencyId, amount);
			}, transaction);
		}

		public Promise<Unit> AddCurrency(CurrencyRef currency, long amount, string transaction = null)
		{
			return AddCurrency(currency.Id, amount, transaction);
		}

		public Promise<Unit> SetCurrencies(Dictionary<CurrencyRef, long> currencyToAmount, string transaction = null)
		{
			return SetCurrencies(currencyToAmount.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value), transaction);
		}

		public Promise<Unit> AddCurrencies(Dictionary<string, long> currencyIdsToAmount, string transaction = null)
		{
			return Update(builder =>
			{
				foreach (var currency in currencyIdsToAmount)
				{
					string currencyId = currency.Key;
					long amount = currency.Value;

					builder.CurrencyChange(currencyId, amount);
				}
			}, transaction);
		}

		public Promise<Unit> AddCurrencies(Dictionary<CurrencyRef, long> currencyToAmount, string transaction = null)
		{
			return AddCurrencies(currencyToAmount.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value), transaction);
		}

		public Promise<Unit> SetCurrency(string currencyId, long amount, string transaction = null)
		{
			return SetCurrencies(new Dictionary<string, long> { { currencyId, amount } }, transaction);
		}

		public Promise<Unit> SetCurrencies(Dictionary<string, long> currencyIdsToAmount, string transaction = null)
		{
			return GetCurrencies(currencyIdsToAmount.Keys.ToArray()).FlatMap(existingAmounts =>
			{
				var deltas = new Dictionary<string, long>();
				foreach (var kvp in currencyIdsToAmount)
				{
					var delta = kvp.Value;
					if (existingAmounts.TryGetValue(kvp.Key, out var existing))
					{
						delta = kvp.Value - existing;
					}

					if (deltas.ContainsKey(kvp.Key))
					{
						deltas[kvp.Key] = delta;
					}
					else
					{
						deltas.Add(kvp.Key, delta);
					}
				}

				return AddCurrencies(deltas, transaction);
			});
		}

		public Promise<Dictionary<string, long>> GetCurrencies(string[] currencyIds)
		{
			string scopes;
			if (currencyIds == null || currencyIds.Count() == 0)
			{
				scopes = "currency";
			}
			else
			{
				scopes = string.Join(",", currencyIds);
			}

			return Requester.Request<InventoryResponse>(Method.GET, CreateRefreshUrl(scopes)).Map(view =>
			{
				return view.currencies.ToDictionary(v => v.id, v => v.amount);
			});
		}

		public Promise<Dictionary<CurrencyRef, long>> GetCurrencies(CurrencyRef[] currencyRefs)
		{
			return GetCurrencies(currencyRefs.Select(r => r.Id).ToArray()).Map(dict =>
			{
				return dict.ToDictionary(kvp => new CurrencyRef(kvp.Key), kvp => kvp.Value);
			});
		}

		protected string CreateRefreshUrl(string scope)
		{
			var queryArgs = "";
			if (!string.IsNullOrEmpty(scope))
			{
				queryArgs = $"?scope={scope}";
			}

			return $"/{SERVICE_OBJECT}/{UserContext.UserId}{queryArgs}";
		}

		public Promise<long> GetCurrency(string currencyId)
		{
			return GetCurrencies(new[] { currencyId }).Map(all =>
			  {
				  if (!all.TryGetValue(currencyId, out var result))
				  {
					  result = 0;
				  }

				  return result;
			  });
		}

		public Promise<Unit> Update(Action<InventoryUpdateBuilder> action, string transaction = null)
		{
			var builder = new InventoryUpdateBuilder();
			action.Invoke(builder);

			return Update(builder, transaction);
		}

		public Promise<Unit> Update(InventoryUpdateBuilder builder, string transaction = null)
		{
			if (builder.IsEmpty)
			{
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				var dict = new ArrayDict();
				if (!string.IsNullOrEmpty(transaction))
				{
					dict.Add("transaction", transaction);
				}

				if (builder.applyVipBonus.HasValue)
				{
					dict.Add("applyVipBonus", builder.applyVipBonus.Value);
				}

				if (builder.currencies != null && builder.currencies.Count > 0)
				{
					dict.Add("currencies", builder.currencies);
				}

				if (builder.currencyProperties != null && builder.currencyProperties.Count > 0)
				{
					var currencyDict = new ArrayDict();
					foreach (var kvp in builder.currencyProperties)
					{
						var newProperties = kvp.Value.Select(newProperty => new ArrayDict
						{
							{"name", newProperty.name},
							{"value", newProperty.value}
						}).ToArray();
						currencyDict.Add(kvp.Key, newProperties);
					}

					dict.Add("currencyProperties", currencyDict);
				}

				if (builder.newItems != null && builder.newItems.Count > 0)
				{
					var newItems = builder.newItems.Select(newItem => new ArrayDict
					{
						{"contentId", newItem.contentId},
						{
							"properties",
							newItem.properties
								   ?.Select(
									   kvp => new ArrayDict
									   {
										   {"name", kvp.Key},
										   {"value", kvp.Value}
									   }).ToArray() ?? new object[] { }
						}
					}).ToArray();

					dict.Add("newItems", newItems);
				}

				if (builder.deleteItems != null && builder.deleteItems.Count > 0)
				{
					var deleteItems = builder.deleteItems.Select(deleteItem => new ArrayDict
					{
						{"contentId", deleteItem.contentId},
						{"id", deleteItem.itemId}
					}).ToArray();

					dict.Add("deleteItems", deleteItems);
				}

				if (builder.updateItems != null && builder.updateItems.Count > 0)
				{
					var updateItems = builder.updateItems.Select(updateItem => new ArrayDict
					{
						{"contentId", updateItem.contentId},
						{"id", updateItem.itemId},
						{
							"properties",
							updateItem.properties
									  .Select(kvp => new ArrayDict
									  {
										  {"name", kvp.Key},
										  {"value", kvp.Value}
									  }).ToArray()
						}
					}).ToArray();

					dict.Add("updateItems", updateItems);
				}

				var json = Json.Serialize(dict, pooledBuilder.Builder);
				return Requester.Request<EmptyResponse>(Method.PUT, CreateRefreshUrl(null), json).ToUnit();
			}
		}

		private Promise<List<InventoryObject<TContent>>> ViewToItems<TContent>(
			InventoryView view,
			IEnumerable<string> filter = null) where TContent : ItemContent, new()
		{
			var filterSet = filter?.ToList();
			var typeName = ContentRegistry.GetContentTypeName(typeof(TContent));

			return Promise.Sequence(view.items
										.Where(kvp => kvp.Key.StartsWith(typeName))
										.Where(kvp => filterSet?.Any(filterId => kvp.Key.StartsWith(filterId)) ?? true)
										.Select(kvp =>
										{
											return ContentApi.Instance
															 .FlatMap(
																 service => service.GetContent<TContent>(
																	 new ItemRef(kvp.Key)))
															 .Map(content =>
															 {
																 return kvp.Value
																		   .Select(item => new InventoryObject<TContent>
																		   {
																			   Id = item.id,
																			   Properties = item.properties,
																			   ItemContent = content,
																			   CreatedAt = item.createdAt,
																			   UpdatedAt = item.updatedAt
																		   }).ToList();
															 });
										}).ToList()).Map(itemGroups =>
			{
				return itemGroups.SelectMany(x => x).Where(x => x != null).ToList();
			});
		}

		public Promise<List<InventoryObject<TContent>>> GetItems<TContent>() where TContent : ItemContent, new()
		{
			// this is the same as running a getCurrent with a certain scope.
			var typeName = ContentRegistry.GetContentTypeName(typeof(TContent));
			return GetCurrent(typeName).FlatMap(view => ViewToItems<TContent>(view));
		}

		public Promise<List<InventoryObject<TContent>>> GetItems<TContent>(params ItemRef<TContent>[] itemReferences)
			where TContent : ItemContent, new()
		{
			var idFilter = itemReferences.Select(r => r.Id).ToList();
			var scope = string.Join(",", idFilter);
			return GetCurrent(scope).FlatMap(view => ViewToItems<TContent>(view, idFilter));
		}

		/// <summary>
		/// Get the current data from the given scope.
		/// </summary>
		/// <param name="scope"></param>
		/// <returns></returns>
		public abstract Promise<InventoryView> GetCurrent(string scope = "");
	}
}
