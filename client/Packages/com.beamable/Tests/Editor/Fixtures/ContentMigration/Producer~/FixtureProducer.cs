using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Shop;
using Beamable.Editor.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class FixtureProducer
{
    [Serializable] public class Record { public string name; public string sha256; }
    [Serializable] public class Report { public string unityVersion; public string sourceCommit; public List<Record> assets = new List<Record>(); }

    [Test]
    public void SaveFixtures()
    {
        const string folder = "Assets/FixtureProduction";
        Assert.That(AssetDatabase.IsValidFolder(folder), Is.False, "Use a fresh producer project.");
        var previous = EditorSettings.serializationMode;
        var report = new Report { unityVersion = Application.unityVersion, sourceCommit = "5b0ca6185444502a7828ce6fe9a3d86b02410643" };
        try
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.CreateFolder("Assets", "FixtureProduction");
            foreach (var state in new[] { "legacy", "empty" })
            {
                var populated = state == "legacy";
                var events = ScriptableObject.CreateInstance<LegacyEvent>();
                events.phases = new List<EventPhase>();
                if (populated) events.phases.Add(new EventPhase { name = "legacy-phase", durationMinutes = 17, rules = new List<EventRule> { new EventRule { rule = "score", value = "nested" } } });
                Save(events, state + "-event", report);
                var game = ScriptableObject.CreateInstance<LegacyGame>();
                game.teams = new List<TeamContent>();
                if (populated) game.teams.Add(new TeamContent { name = "legacy-team", maxPlayers = 7 });
                Save(game, state + "-game", report);
                var shop = ScriptableObject.CreateInstance<LegacyShop>();
                shop.currency = new LegacyCurrency { symbol = populated ? "currency.gold" : "", amount = 42 };
                shop.item = new LegacyItem { contentId = populated ? "items.sword" : "" };
                shop.sku = new LegacySku { itunes = populated ? "apple.id" : "", googleplay = populated ? "google.id" : "" };
                Save(shop, state + "-shop", report);
            }
            var modernEvent = ScriptableObject.CreateInstance<MigrationEventAsset>();
            modernEvent.phases = new PhaseList { listData = new List<EventPhase> { new EventPhase { name = "modern-phase", durationMinutes = 18, rules = new List<EventRule> { new EventRule { rule = "score", value = "nested" } } } } };
            modernEvent.schedule = new OptionalEventSchedule { Value = new EventSchedule { activeFrom = "2020-01-02T03:04:05Z" } };
            Save(modernEvent, "modern-event", report);
            var modernGame = ScriptableObject.CreateInstance<MigrationGameAsset>();
            modernGame.teams = new TeamContentList { listData = new List<TeamContent> { new TeamContent { name = "modern-team", maxPlayers = 8 } } };
            modernGame.federatedGameServerNamespace = new OptionalString();
            modernGame.newFederatedGameServerNamespace = new OptionalNamespace();
            Save(modernGame, "modern-game", report);
            var modernShop = ScriptableObject.CreateInstance<MigrationShopAsset>();
            modernShop.currency = new OfferObtainCurrency { symbol = new CurrencyRef("currency.silver"), amount = 43 };
            modernShop.item = new OfferObtainItem { contentId = new ItemRef("items.shield"), properties = new List<OfferObtainItemProperty> { new OfferObtainItemProperty { name = "tier", value = "rare" } } };
            modernShop.sku = new SKUProductIds { itunes = new OptionalString("apple.modern"), googleplay = new OptionalString("google.modern") };
            Save(modernShop, "modern-shop", report);
            Assert.That(report.assets.Count, Is.EqualTo(9));
            File.WriteAllText("fixture-producer-result.json", JsonUtility.ToJson(report, true));
        }
        finally { EditorSettings.serializationMode = previous; }
    }

    private static void Save(ScriptableObject asset, string name, Report report)
    {
        var path = "Assets/FixtureProduction/" + name + ".asset";
        AssetDatabase.CreateAsset(asset, path);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        Assert.That(File.Exists(path), Is.True);
        using (var sha = SHA256.Create())
            report.assets.Add(new Record { name = name, sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() });
        Resources.UnloadAsset(asset);
    }
}
