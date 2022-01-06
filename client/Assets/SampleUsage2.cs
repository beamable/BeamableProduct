// unset

using Beamable;
using Beamable.Common.Inventory;
using Beamable.Player;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
	public class SampleUsage2 : MonoBehaviour
	{
		public CurrencyRef currency;

		public ItemRef ItemRef;

		public int updateIndex;

		public string propertyKey = "b", propertyValue = "okay";
		// public PlayerItemGroup AllItems;
		public PlayerItemGroup Items;

		public PlayerInventory Inv;


		async void Start()
		{
			var ctx = BeamContext.ForContext(this);

			// AllItems = ctx.Inventory.GetItems();
			// AllItems.OnDataUpdated += (data) =>
			// {
			// 	Debug.Log("ALl Items updated! " + data.Count);
			// 	foreach (var item in data)
			// 	{
			// 		Debug.Log(" " + item.ContentId);
			// 	}
			// };

			Inv = ctx.Inventory;
			Items = ctx.Inventory.GetItems(ItemRef);

			Items.OnElementsAdded += items =>
			{
				foreach (var item in items)
				{
					Debug.Log($"New item! {item.ContentId}/{item.ItemId}");
					item.OnDeleted += () => Debug.Log($"Item has been deleted :( {item.ContentId}/{item.ItemId}");
					item.OnUpdated += () => Debug.Log($"Item has been updated !! {item.ContentId}/{item.ItemId}");
				}
			};
			Items.OnElementRemoved += items => {
				foreach (var item in items)
				{
					Debug.Log($"Item deleted :( {item.ContentId}/{item.ItemId}");
				}
			};

			Items.OnDataUpdated += (data) =>
			{
				Debug.Log("Items updated! " + data.Count);
				foreach (var item in data)
				{
					Debug.Log(" " + item.ContentId);
				}
			};

			await Items.Refresh();
			foreach (var item in Items)
			{
				Debug.Log($"Starting item! {item.ContentId}/{item.ItemId}");
			}

		}

		[ContextMenu("Add Currency")]
		public async void AddCurrency()
		{
			var ctx = BeamContext.ForContext(this);

			await ctx.Inventory.Update(builder =>
			{
				builder.CurrencyChange(currency, 3);
			});


			Debug.Log("Currency updated to " + ctx.Inventory.GetCurrency("currency.gems"));
		}

		[ContextMenu("Add Item")]
		public async void AddBowItem()
		{
			var ctx = BeamContext.ForContext(this);

			await ctx.Inventory.Update(b => b.AddItem(ItemRef, new Dictionary<string, string> {["owner"] = "demo"}));

			Debug.Log("items added to " + ctx.Inventory.GetItems(ItemRef).Count);
		}

		[ContextMenu("Modify First Item")]
		public async void UpdateBowItem()
		{
			var ctx = BeamContext.ForContext(this);


			await ctx.Inventory.Update(b =>
			{
				var item = ctx.Inventory.GetItems(ItemRef)[updateIndex];
				// b.UpdateItem(ItemRef, item.ItemId, item.Properties);
				b.UpdateItem(ItemRef, item.ItemId,
				             new Dictionary<string, string>()
				             {
					             ["a"] = Time.realtimeSinceStartup.ToString("00000"),
					             [propertyKey] = propertyValue
				             });
			});

			Debug.Log("items updated to " + ctx.Inventory.GetItems(ItemRef).Count);
		}

		[ContextMenu("Delete First Item")]
		public async void DeleteFirstItem()
		{
			var ctx = BeamContext.ForContext(this);

			await ctx.Inventory.Update(b =>
			{
				var item = ctx.Inventory.GetItems(ItemRef)[updateIndex];
				b.DeleteItem(item.ContentId, item.ItemId);
			});

			Debug.Log("items deleted to " + ctx.Inventory.GetItems(ItemRef).Count);
		}
	}
}
