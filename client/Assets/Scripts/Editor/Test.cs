using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Editor.Content;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Assets.Editor
{
	public class Test
	{

		[MenuItem("Window/Make new Content")]
		public static void CheckContent()
		{
			var manager = new ContentManager();
			manager.Initialize();
			var itemType = typeof(ItemContent);
			var currencyType = typeof(CurrencyContent);
			for (int i = 0; i < 100; i++)
			{
				var itemName = $"TEST_ITEM_{i}";
				ContentObject content = ScriptableObject.CreateInstance(itemType) as ContentObject;
				content.SetContentName(itemName);
				content.LastChanged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

				manager.Model.CreateItem(content);
				var currency_name = $"TEST_CURRENCY_{i}";
				ContentObject content2 = ScriptableObject.CreateInstance(currencyType) as ContentObject;
				content2.SetContentName(currency_name);
				content2.LastChanged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

				manager.Model.CreateItem(content2);
			}
		}

	}
}
