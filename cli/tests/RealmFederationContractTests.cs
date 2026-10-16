using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Realms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests;

public class RealmFederationContractTests
{
    [Test]
    public void RealmPayloadAndViewKeepConfigDictionary()
    {
        var config = new Dictionary<string, string> { ["namespace.key"] = "value", ["empty"] = "" };
        var dto = JsonConvert.DeserializeObject<GetRealmConfigDTO>(JsonConvert.SerializeObject(new GetRealmConfigDTO { config = config }));
        var view = JsonConvert.DeserializeObject<RealmConfigView>(JsonConvert.SerializeObject(new RealmConfigView { Namespace = "namespace", Config = config }));
        Assert.That(dto.config["namespace.key"], Is.EqualTo("value"));
        Assert.That(view.Config["empty"], Is.EqualTo(""));
        Assert.That(view.Namespace, Is.EqualTo("namespace"));
    }

    [Test]
    public void FederationPayloadKeepsNestedPlayerInventoryAndMessageFields()
    {
        var server = new ServerInfo();
        server.playerData["42"] = new Dictionary<string, string> { ["token"] = "player-token" };
        var serverPayload = JObject.Parse(JsonConvert.SerializeObject(server));
        Assert.That((string)serverPayload["playerData"]["42"]["token"], Is.EqualTo("player-token"));
        var inventory = new FederatedInventoryProxyState
        {
            currencies = new Dictionary<string, long> { ["currency.coins"] = 9007199254740993L },
            items = new Dictionary<string, List<FederatedItemProxy>> { ["items.sword"] = new List<FederatedItemProxy> { new FederatedItemProxy { proxyId = "external-17", properties = new List<ItemProperty> { new ItemProperty { name = "tier", value = "rare" } } } } }
        };
        var restoredInventory = JsonConvert.DeserializeObject<FederatedInventoryProxyState>(JsonConvert.SerializeObject(inventory));
        Assert.That(restoredInventory.currencies["currency.coins"], Is.EqualTo(9007199254740993L));
        Assert.That(restoredInventory.items["items.sword"][0].properties[0].value, Is.EqualTo("rare"));
        var message = new MessageRailSendResponse { sentPlayers = new List<string> { "42" }, maxBatchSize = 20 };
        message.sentPayloadFed["42"] = "provider-id";
        message.@params["template"] = "welcome";
        var payload = JsonConvert.SerializeObject(message);
        Assert.That((string)JObject.Parse(payload)["params"]["template"], Is.EqualTo("welcome"));
        var restoredMessage = JsonConvert.DeserializeObject<MessageRailSendResponse>(payload);
        Assert.That(restoredMessage.sentPayloadFed["42"], Is.EqualTo("provider-id"));
        Assert.That(restoredMessage.maxBatchSize, Is.EqualTo(20));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FederationInventoryKeepsEmptyVersusNullDictionaries(bool useNull)
    {
        var value = new FederatedInventoryProxyState { currencies = useNull ? null : new Dictionary<string, long>(), items = useNull ? null : new Dictionary<string, List<FederatedItemProxy>>() };
        var payload = JObject.Parse(JsonConvert.SerializeObject(value));
        Assert.That(payload["currencies"].Type, Is.EqualTo(useNull ? JTokenType.Null : JTokenType.Object));
        Assert.That(payload["items"].Type, Is.EqualTo(useNull ? JTokenType.Null : JTokenType.Object));
    }
}
