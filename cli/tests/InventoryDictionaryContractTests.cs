using System.Collections.Generic;
using System.Text;
using Beamable.Common.Api.Events;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Serialization.SmallerJSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests;

public class InventoryDictionaryContractTests
{
    [TestCase(null)]
    [TestCase(0L)]
    [TestCase(9007199254740993L)]
    public void LegacyInventoryRequestRetainsCurrencies(long? amount)
    {
        var request = new InventoryUpdateRequest { transaction = "transaction", currencies = amount.HasValue ? new Dictionary<string, long> { ["currency.coins"] = amount.Value } : null };
        var json = Json.Serialize(request, new StringBuilder());
        var payload = JObject.Parse(json);
        Assert.That(payload.ContainsKey("currencies"), Is.True);
        var restored = JsonConvert.DeserializeObject<InventoryUpdateRequest>(json);
        Assert.That(restored.transaction, Is.EqualTo("transaction"));
        if (amount.HasValue) Assert.That(restored.currencies["currency.coins"], Is.EqualTo(amount.Value));
        else Assert.That(restored.currencies, Is.Null);
    }

    [Test]
    public void InventoryPayloadRetainsNestedCurrencyAndItemProperties()
    {
        var view = new InventoryView { scope = "player" };
        view.currencies["currency.coins"] = 9007199254740993L;
        view.currencyProperties["currency.coins"] = new List<CurrencyProperty> { new CurrencyProperty { name = "source", value = "reward" } };
        view.items["items.sword"] = new List<ItemView> { new ItemView { id = 17, properties = new Dictionary<string, string> { ["tier"] = "rare" }, createdAt = 7, updatedAt = 9 } };
        var restored = Json.Deserialize<InventoryView>(Json.Serialize(view, new StringBuilder()));
        Assert.That(restored.currencies["currency.coins"], Is.EqualTo(9007199254740993L));
        Assert.That(restored.currencyProperties["currency.coins"][0].value, Is.EqualTo("reward"));
        Assert.That(restored.items["items.sword"][0].properties["tier"], Is.EqualTo("rare"));
        Assert.That(restored.items["items.sword"][0].updatedAt, Is.EqualTo(9));
        Assert.That(restored.scope, Is.EqualTo("player"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyAndNullDictionariesRemainDistinct(bool useNull)
    {
        var value = useNull ? null : new Dictionary<string, string>();
        var eventItem = new EventItem { id = "items.sword", properties = value };
        var inventoryObject = new InventoryObject<ItemContent> { Id = 17, Properties = value };
        var lobby = new Lobby { lobbyId = "lobby", restriction = "Open", data = value, players = new List<LobbyPlayer> { new LobbyPlayer { playerId = "42", stats = value } } };
        foreach (var pair in new[] { (Json.Serialize(eventItem, new StringBuilder()), "properties"), (Json.Serialize(inventoryObject, new StringBuilder()), "Properties"), (JsonConvert.SerializeObject(lobby), "data") })
            Assert.That(JObject.Parse(pair.Item1)[pair.Item2].Type, Is.EqualTo(useNull ? JTokenType.Null : JTokenType.Object));
        var restored = JsonConvert.DeserializeObject<Lobby>(JsonConvert.SerializeObject(lobby));
        Assert.That(restored.players[0].stats == null, Is.EqualTo(useNull));
    }

    [Test]
    public void EventInventoryAndLobbyJsonKeepPopulatedProperties()
    {
        var properties = new Dictionary<string, string> { ["key"] = "value" };
        Assert.That(Json.Deserialize<EventItem>(Json.Serialize(new EventItem { properties = properties }, new StringBuilder())).properties["key"], Is.EqualTo("value"));
        Assert.That(Json.Deserialize<InventoryObject<ItemContent>>(Json.Serialize(new InventoryObject<ItemContent> { Properties = properties }, new StringBuilder())).Properties["key"], Is.EqualTo("value"));
        var lobby = new Lobby { restriction = "Open", data = properties, players = new List<LobbyPlayer> { new LobbyPlayer { stats = properties } } };
        var restored = JsonConvert.DeserializeObject<Lobby>(JsonConvert.SerializeObject(lobby));
        Assert.That(restored.data["key"], Is.EqualTo("value"));
        Assert.That(restored.players[0].stats["key"], Is.EqualTo("value"));
    }
}
