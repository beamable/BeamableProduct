using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using Beamable.Server.Common;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests;

public class InventoryUpdateBuilderTests
{
	[Test]
	public void DeserializeNetworkJson()
	{
		var settings = UnitySerializationSettings.Instance;
		var json = @"{""newItems"":[{""reqId"":""fa4a8932-a255-4276-ada2-fd918a82801f"",""contentId"":""items.salmon"",""properties"":[{""name"":""a"",""value"":""v1""},{""name"":""b"",""value"":""v2""}]}]}";
		JsonConvert.DeserializeObject(json, typeof(InventoryUpdateBuilder), settings);

		// the test here is that this doesn't explode... Because we shouldn't be using newtonsoft to deserializing the network variant...
	}

	[Test]
	public void Serialize()
	{
		var settings = UnitySerializationSettings.Instance;
		var builder = new InventoryUpdateBuilder().AddItem(new ItemRef("items.tuna"));
		var json = JsonConvert.SerializeObject(builder, settings);
		Assert.IsTrue(json.Contains("items.tuna"));
	}

	[Test]
	public void CallMethods()
	{
		for (var i = 0; i < 100; i++)
		{
			var instance = new HasMethodsToRun();
			var json = JsonConvert.SerializeObject(instance, UnitySerializationSettings.Instance);
			Assert.IsTrue(json.Contains("1"));
			instance = JsonConvert.DeserializeObject<HasMethodsToRun>(json, UnitySerializationSettings.Instance);
			Assert.IsTrue(instance.x == 0);
		}
	}

	public class HasMethodsToRun : ISerializationCallbackReceiver
	{
		public int x;
		public void OnBeforeSerialize()
		{
			x++;
		}

		public void OnAfterDeserialize()
		{
			x--;
		}
	}


	[Test]
	public void DeserializeUnityJson()
	{
		var settings = UnitySerializationSettings.Instance;

		var json = @"{
    ""_serializedApplyVipBonus"": 0,
    ""_serializedCurrencies"": {
        ""keys"": [],
        ""values"": []
    },
    ""_serializedCurrencyProperties"": {
        ""keys"": [],
        ""values"": []
    },
    ""_serializedNewItems"": [
        {
            ""contentId"": ""items.salmon"",
            ""properties"": {
                ""keys"": [],
                ""values"": []
            },
            ""requestId"": ""003c34a0-2b7f-4689-bc15-ee7182e2aef2""
        }
    ],
    ""_serializedDeleteItems"": [],
    ""_serializedUpdateItems"": []
}";

		var builder = JsonConvert.DeserializeObject<InventoryUpdateBuilder>(json, settings);

		Assert.AreEqual(1, builder.newItems.Count);
		Assert.AreEqual("items.salmon", builder.newItems[0].contentId);
	}
}
