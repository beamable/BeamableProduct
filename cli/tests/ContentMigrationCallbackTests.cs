using System.Collections.Generic;
using System.Reflection;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Shop;
using NUnit.Framework;

namespace tests;

public class ContentMigrationCallbackTests
{
    private static FieldInfo Legacy(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("legacy.id")]
    public void SkuCallbacksMigrateOnlyPopulatedLegacyIds(string legacy)
    {
        var sku = new SKUProductIds { itunes = new OptionalString("current.apple"), googleplay = new OptionalString("current.google") };
        Legacy(sku, "itunes_legacy").SetValue(sku, legacy);
        Legacy(sku, "googleplay_legacy").SetValue(sku, legacy);
        sku.OnAfterDeserialize();
        Assert.That(sku.itunes.Value, Is.EqualTo(string.IsNullOrEmpty(legacy) ? "current.apple" : legacy));
        Assert.That(sku.googleplay.Value, Is.EqualTo(string.IsNullOrEmpty(legacy) ? "current.google" : legacy));
        sku.OnBeforeSerialize();
        Assert.That(Legacy(sku, "itunes_legacy").GetValue(sku), Is.Null);
        Assert.That(Legacy(sku, "googleplay_legacy").GetValue(sku), Is.Null);
        sku.OnAfterDeserialize();
        Assert.That(sku.itunes.HasValue, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("legacy.id")]
    public void OfferCallbacksPreserveCurrentReferencesOrMigrateLegacy(string legacy)
    {
        var currency = new OfferObtainCurrency { symbol = new CurrencyRef("currency.current"), amount = 5 };
        var item = new OfferObtainItem { contentId = new ItemRef("items.current"), properties = new List<OfferObtainItemProperty> { new OfferObtainItemProperty { name = "tier", value = "rare" } } };
        Legacy(currency, "symbol_legacy").SetValue(currency, legacy);
        Legacy(item, "content_legacy").SetValue(item, legacy);
        currency.OnAfterDeserialize();
        item.OnAfterDeserialize();
        Assert.That(currency.symbol.Id, Is.EqualTo(string.IsNullOrEmpty(legacy) ? "currency.current" : legacy));
        Assert.That(item.contentId.Id, Is.EqualTo(string.IsNullOrEmpty(legacy) ? "items.current" : legacy));
        Assert.That(item.properties[0].value, Is.EqualTo("rare"));
        Assert.That(currency.amount, Is.EqualTo(5));
        currency.OnBeforeSerialize();
        item.OnBeforeSerialize();
        Assert.That(Legacy(currency, "symbol_legacy").GetValue(currency), Is.Null);
        Assert.That(Legacy(item, "content_legacy").GetValue(item), Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EventAndTeamCallbacksMigratePopulatedLegacyLists(bool populated)
    {
        var events = new EventContent { phases = new PhaseList { listData = new List<EventPhase> { new EventPhase { name = "current", durationMinutes = 7 } } } };
        var legacyPhases = new List<EventPhase>();
        if (populated) legacyPhases.Add(new EventPhase { name = "legacy", durationMinutes = 11 });
        Legacy(events, "legacyPhases").SetValue(events, legacyPhases);
        events.OnAfterDeserialize();
        Assert.That(events.phases.listData[0].name, Is.EqualTo(populated ? "legacy" : "current"));
        Assert.That(events.phases.listData[0].durationMinutes, Is.EqualTo(populated ? 11 : 7));
        Assert.That(Legacy(events, "legacyPhases").GetValue(events), Is.Null);
        events.OnBeforeSerialize();
        events.OnAfterDeserialize();
        Assert.That(events.phases.listData.Count, Is.EqualTo(1));

        var game = new SimGameType { teams = new TeamContentList { listData = new List<TeamContent> { new TeamContent { name = "current", maxPlayers = 2 } } }, legacyTeams = new List<TeamContent>(), federatedGameServerNamespace = new OptionalString(), newFederatedGameServerNamespace = new OptionalNamespace() };
        if (populated) game.legacyTeams.Add(new TeamContent { name = "legacy", maxPlayers = 4 });
        game.OnAfterDeserialize();
        Assert.That(game.teams.listData[0].name, Is.EqualTo(populated ? "legacy" : "current"));
        Assert.That(game.CalculateMaxPlayers(), Is.EqualTo(populated ? 4 : 2));
        Assert.That(game.legacyTeams, Is.Null);
        game.OnBeforeSerialize();
        game.OnAfterDeserialize();
        Assert.That(game.CalculateMaxPlayers(), Is.EqualTo(populated ? 4 : 2));
    }
}
