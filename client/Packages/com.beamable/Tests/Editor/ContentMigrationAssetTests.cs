using System;
using System.IO;
using Beamable.Editor.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentMigrationAssetTests
	{
		private string _folder;
		private SerializationMode _serializationMode;

		[SetUp]
		public void SetUp()
		{
			_serializationMode = EditorSettings.serializationMode;
			EditorSettings.serializationMode = SerializationMode.ForceText;
			_folder = "Assets/BeamableMigrationTests-" + Guid.NewGuid().ToString("N");
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
		}

		[TearDown]
		public void TearDown()
		{
			try { if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder); }
			finally { EditorSettings.serializationMode = _serializationMode; }
		}

		[TestCase("legacy")]
		[TestCase("modern")]
		[TestCase("empty")]
		public void EventPhasesSurviveImportAndReload(string state)
		{
			var asset = Import<MigrationEventAsset>(state + "-event");
			AssertEvent(asset, state);
			AssertEvent(SaveAndReload(asset), state);
		}

		[TestCase("legacy")]
		[TestCase("modern")]
		[TestCase("empty")]
		public void GameTeamsSurviveImportAndReload(string state)
		{
			var asset = Import<MigrationGameAsset>(state + "-game");
			AssertGame(asset, state);
			AssertGame(SaveAndReload(asset), state);
		}

		[TestCase("legacy")]
		[TestCase("modern")]
		[TestCase("empty")]
		public void ShopReferencesAndStoreIdsSurviveImportAndReload(string state)
		{
			var asset = Import<MigrationShopAsset>(state + "-shop");
			AssertShop(asset, state);
			AssertShop(SaveAndReload(asset), state);
		}

		[TestCase("event", "phases")]
		[TestCase("game", "teams")]
		[TestCase("shop", "symbol")]
		[TestCase("shop", "contentId")]
		[TestCase("shop", "itunes")]
		[TestCase("shop", "googleplay")]
		public void UnrecognizedLegacyNameDoesNotMigrate(string kind, string field)
		{
			// A misspelled old name must lose the value, proving the fixture exercises alias resolution.
			if (kind == "event")
				Assert.That(Import<MigrationEventAsset>("legacy-event", field).phases.listData, Is.Empty);
			else if (kind == "game")
				Assert.That(Import<MigrationGameAsset>("legacy-game", field).teams.listData, Is.Empty);
			else
			{
				var asset = Import<MigrationShopAsset>("legacy-shop", field);
				switch (field)
				{
					case "symbol": Assert.That(asset.currency.symbol.Id, Is.Null.Or.Empty); break;
					case "contentId": Assert.That(asset.item.contentId.Id, Is.Null.Or.Empty); break;
					case "itunes": Assert.That(asset.sku.itunes.HasValue, Is.False); break;
					case "googleplay": Assert.That(asset.sku.googleplay.HasValue, Is.False); break;
				}
			}
		}

		private T Import<T>(string fixture, string unknownField = null) where T : ScriptableObject
		{
			var instance = ScriptableObject.CreateInstance<T>();
			string scriptPath;
			try { scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance)); }
			finally { UnityEngine.Object.DestroyImmediate(instance); }
			Assert.That(scriptPath, Is.Not.Empty);
			var fixturePath = Path.GetDirectoryName(scriptPath).Replace('\\', '/') + "/" + fixture + ".yaml.txt";
			var text = AssetDatabase.LoadAssetAtPath<TextAsset>(fixturePath);
			Assert.That(text, Is.Not.Null, fixturePath);
			var yaml = text.text.Replace("__SCRIPT_GUID__", AssetDatabase.AssetPathToGUID(scriptPath));
			if (unknownField != null)
			{
				Assert.That(yaml, Does.Contain(" " + unknownField + ":"));
				yaml = yaml.Replace(" " + unknownField + ":", " unknown_" + unknownField + ":");
			}
			var path = _folder + "/" + fixture + ".asset";
			File.WriteAllText(path, yaml);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
			var asset = AssetDatabase.LoadAssetAtPath<T>(path);
			Assert.That(asset, Is.Not.Null, path);
			return asset;
		}

		private static T SaveAndReload<T>(T asset) where T : ScriptableObject
		{
			var path = AssetDatabase.GetAssetPath(asset);
			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssetIfDirty(asset);
			Resources.UnloadAsset(asset);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
			var restored = AssetDatabase.LoadAssetAtPath<T>(path);
			Assert.That(restored, Is.Not.Null);
			return restored;
		}

		private static void AssertEvent(MigrationEventAsset asset, string state)
		{
			Assert.That(asset.phases.listData, Has.Count.EqualTo(state == "empty" ? 0 : 1));
			if (state == "empty") return;
			var phase = asset.phases.listData[0];
			Assert.That(phase.name, Is.EqualTo(state + "-phase"));
			Assert.That(phase.durationMinutes, Is.EqualTo(state == "legacy" ? 17 : 18));
			Assert.That(phase.rules[0].rule, Is.EqualTo("score"));
			Assert.That(phase.rules[0].value, Is.EqualTo("nested"));
		}

		private static void AssertGame(MigrationGameAsset asset, string state)
		{
			Assert.That(asset.teams.listData, Has.Count.EqualTo(state == "empty" ? 0 : 1));
			if (state == "empty") return;
			Assert.That(asset.teams.listData[0].name, Is.EqualTo(state + "-team"));
			Assert.That(asset.CalculateMaxPlayers(), Is.EqualTo(state == "legacy" ? 7 : 8));
			Assert.That(asset.teams.listData[0].minPlayers.HasValue, Is.False);
		}

		private static void AssertShop(MigrationShopAsset asset, string state)
		{
			var empty = state == "empty";
			var legacy = state == "legacy";
			Assert.That(asset.currency.amount, Is.EqualTo(state == "modern" ? 43 : 42));
			if (empty)
			{
				Assert.That(asset.currency.symbol.Id, Is.Null.Or.Empty);
				Assert.That(asset.item.contentId.Id, Is.Null.Or.Empty);
			}
			else
			{
				Assert.That(asset.currency.symbol.Id, Is.EqualTo(legacy ? "currency.gold" : "currency.silver"));
				Assert.That(asset.item.contentId.Id, Is.EqualTo(legacy ? "items.sword" : "items.shield"));
			}
			Assert.That(asset.sku.itunes.HasValue, Is.EqualTo(!empty));
			Assert.That(asset.sku.googleplay.HasValue, Is.EqualTo(!empty));
			if (empty)
			{
				Assert.That(asset.sku.itunes.Value, Is.Null.Or.Empty);
				Assert.That(asset.sku.googleplay.Value, Is.Null.Or.Empty);
			}
			else
			{
				Assert.That(asset.sku.itunes.Value, Is.EqualTo(legacy ? "apple.id" : "apple.modern"));
				Assert.That(asset.sku.googleplay.Value, Is.EqualTo(legacy ? "google.id" : "google.modern"));
			}
			if (state == "modern")
			{
				Assert.That(asset.item.properties[0].name, Is.EqualTo("tier"));
				Assert.That(asset.item.properties[0].value, Is.EqualTo("rare"));
			}
		}
	}
}
