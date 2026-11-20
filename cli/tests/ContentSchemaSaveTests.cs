using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using cli;
using cli.Content;
using NUnit.Framework;

namespace tests;

[NonParallelizable]
public class ContentSchemaSaveTests
{
    private string root = null!, path = null!;
    private ContentService service = null!;
    private const string Id = "items.sword";
    private const string Model = "{\"damage\":{\"data\":20},\"added\":{\"data\":0}}";
    private const string Edited = "{\"damage\":{\"data\":21},\"added\":{\"data\":0}}";
    private const string Properties = "{\"damage\":{\"data\":20},\"future\":{\"data\":18446744073709551615}}";
    private static string FileJson(string properties) => "{\"properties\":" + properties + ",\"tags\":[\"mine\"],\"referenceManifestId\":\"publication\",\"baseline\":\"checksum@v1\"}";
    private LocalContentFiles Cache => new() { ManifestId = "global", PerIdContentFiles = new Dictionary<string, ContentFile>() };

    [SetUp] public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "beam-schema-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ConfigService.CFG_FOLDER));
        var config = new ConfigService(null); config.SetWorkingDir(root);
        var requester = new CliRequester(new RequesterInfo("schema-cid", "schema-pid", "fake", "fake", "http://127.0.0.1:1"));
        service = new ContentService(requester, config, null!, null!, null!, null!, null!);
        path = Path.Combine(service.EnsureContentPathForRealmExists(out _, "schema-pid"), Id + ".json");
    }
    [TearDown] public void TearDown() => Directory.Delete(root, true);
    private Task Save(string proposed, string? original = null, string? model = null, string? hash = null) =>
        service.BulkSaveLocalContent(Cache, new[] { Id }, new[] { proposed },
            originalFiles: original == null ? null : new[] { Convert.ToBase64String(Encoding.UTF8.GetBytes(original)) }, modelProperties: model == null ? null : new[] { model },
            expectedFileHashes: hash == null ? null : new[] { hash });

    [Test] public void Formatted_original_file_survives_the_server_command_splitter()
    {
        var original = "{\n  \"properties\": {\"text\": {\"data\": \"quote \\\" and space\"}},\n  \"tags\": []\n}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        var arguments = System.Linq.Enumerable.ToArray(App.ServerCLIStringSplitter.Instance.Split("content save --original-files=" + encoded + " --model-properties={} --content-properties={}"));
        Assert.That(arguments.Length, Is.EqualTo(5));
        Assert.That(Encoding.UTF8.GetString(Convert.FromBase64String(arguments[2].Substring("--original-files=".Length))), Is.EqualTo(original));
    }

    [ContentType("schemaSaveOld")]
    public class OldModel : ContentObject { public int damage; }
    [ContentType("schemaSaveNew")]
    public class NewModel : ContentObject { public int damage; public int future; }

    [Test] public async Task Serializer_produced_new_model_round_trips_through_old_model_edit()
    {
        var instanceField = typeof(ContentTypeReflectionCache).GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var previous = instanceField.GetValue(null);
        try
        {
            const string producerId = "schemaSaveNew.sword";
            var cache = new ContentTypeReflectionCache();
            cache.OnSetupForCacheGeneration();
            cache.OnBaseTypeOfInterestFound(cache.BaseTypesOfInterest[0], new[] { typeof(OldModel), typeof(NewModel) });
            var newer = new NewModel { damage = 20, future = 37 };
            newer.SetIdAndVersion(producerId, "v1");
            var older = new OldModel();
            // Produce both the model properties and the working-file envelope through their owning serializers.
            await service.SaveContentFile(Path.GetDirectoryName(path)!, new ContentFile
            {
                Id = Id, Properties = JsonSerializer.Deserialize<JsonElement>(ClientContentSerializer.SerializeProperties(newer)),
                Tags = JsonSerializer.SerializeToElement(new[] { "mine" }), FetchedFromManifestUid = "publication"
            });
            var original = File.ReadAllText(path);
            ClientContentSerializer.DeserializeContentFromCli(original, older, producerId, out var difference);
            Assert.That(difference.HasFlag(SchemaDifference.UnknownFieldsInJson), Is.True);
            var before = ClientContentSerializer.SerializeProperties(older);
            older.damage = 21;
            await File.WriteAllTextAsync(path, original);
            await Save(ClientContentSerializer.SerializeProperties(older), original, before);
            var readBack = new NewModel();
            ClientContentSerializer.DeserializeContentFromCli(File.ReadAllText(path), readBack, producerId, out _);
            Assert.That(readBack.damage, Is.EqualTo(21));
            Assert.That(readBack.future, Is.EqualTo(37));
        }
        finally { instanceField.SetValue(null, previous); }
    }

    [Test] public async Task Edit_preserves_unknown_numbers_missing_defaults_tags_and_baseline()
    {
        var original = FileJson(Properties); await File.WriteAllTextAsync(path, original);
        await Save(Edited, original, Model);
        using var saved = JsonDocument.Parse(File.ReadAllText(path));
        var props = saved.RootElement.GetProperty("properties");
        Assert.That(props.GetProperty("damage").GetProperty("data").GetInt32(), Is.EqualTo(21));
        Assert.That(props.GetProperty("future").GetProperty("data").GetUInt64(), Is.EqualTo(ulong.MaxValue));
        Assert.That(props.TryGetProperty("added", out _), Is.False);
        Assert.That(saved.RootElement.GetProperty("tags")[0].GetString(), Is.EqualTo("mine"));
        Assert.That(saved.RootElement.GetProperty("baseline").GetString(), Is.EqualTo("checksum@v1"));
        Assert.That(saved.RootElement.GetProperty("referenceManifestId").GetString(), Is.EqualTo("publication"));
    }

    [TestCase(false)] [TestCase(true)] public async Task Equivalent_save_preserves_bytes_and_timestamp(bool patch)
    {
        var original = FileJson(Properties); await File.WriteAllTextAsync(path, original);
        File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var stamp = File.GetLastWriteTimeUtc(path);
        await Save(patch ? Model : "{\"future\":{\"data\":18446744073709551615}, \"damage\":{\"data\":20}}", patch ? original : null, patch ? Model : null);
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(stamp));
    }

    [Test] public async Task Concurrent_unrelated_edit_is_preserved_but_same_field_edit_is_rejected()
    {
        var original = FileJson(Properties);
        await File.WriteAllTextAsync(path, original.Replace("18446744073709551615", "99"));
        await Save(Edited, original, Model);
        Assert.That(File.ReadAllText(path), Does.Contain("99"));
        var concurrent = original.Replace(":20", ":30"); await File.WriteAllTextAsync(path, concurrent);
        var stale = Assert.ThrowsAsync<CliException>(() => Save(Edited, original, Model));
        Assert.That(stale.NonZeroOrOneExitCode, Is.EqualTo(ContentPropertyEdits.StaleEditExitCode), "Engines tell a stale edit apart by exit code.");
        Assert.That(File.ReadAllText(path), Is.EqualTo(concurrent));
    }

    [Test] public async Task Nested_object_edit_preserves_unknown_fields()
    {
        const string before = "{\"settings\":{\"data\":{\"damage\":20}}}";
        var original = FileJson("{\"settings\":{\"data\":{\"damage\":20,\"future\":37}}}");
        await File.WriteAllTextAsync(path, original);
        await Save(before.Replace(":20", ":21"), original, before);
        using var saved = JsonDocument.Parse(File.ReadAllText(path));
        Assert.That(saved.RootElement.GetProperty("properties").GetProperty("settings").GetProperty("data").GetProperty("future").GetInt32(), Is.EqualTo(37));
    }

    [TestCase("{\"rows\":{\"data\":[{\"damage\":21}]}}")]
    [TestCase("{}")]
    public async Task Structural_edits_cannot_discard_hidden_data(string edited)
    {
        const string before = "{\"rows\":{\"data\":[{\"damage\":20}]}}";
        var original = FileJson("{\"rows\":{\"data\":[{\"damage\":20,\"future\":37}]}}");
        await File.WriteAllTextAsync(path, original);
        var unsafeEdit = Assert.ThrowsAsync<CliException>(() => Save(edited, original, before));
        Assert.That(unsafeEdit.NonZeroOrOneExitCode, Is.EqualTo(ContentPropertyEdits.UnsafeEditExitCode), "Engines offer the schema review for this exit code.");
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
    }

    [Test] public async Task Known_optional_removal_and_missing_field_edit_work()
    {
        var original = FileJson(Properties); await File.WriteAllTextAsync(path, original);
        await Save("{\"added\":{\"data\":5}}", original, Model);
        using var saved = JsonDocument.Parse(File.ReadAllText(path));
        var props = saved.RootElement.GetProperty("properties");
        Assert.That(props.TryGetProperty("damage", out _), Is.False);
        Assert.That(props.GetProperty("added").GetProperty("data").GetInt32(), Is.EqualTo(5));
        Assert.That(props.TryGetProperty("future", out _), Is.True);
    }

    [Test] public async Task Reviewed_migration_checks_hash_and_preserves_metadata()
    {
        var original = FileJson(Properties); await File.WriteAllTextAsync(path, original);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original))).ToLowerInvariant();
        Assert.ThrowsAsync<CliException>(() => Save(Model, hash: "stale"));
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        await Save(Model, hash: hash);
        using var saved = JsonDocument.Parse(File.ReadAllText(path));
        Assert.That(saved.RootElement.GetProperty("properties").TryGetProperty("future", out _), Is.False);
        Assert.That(saved.RootElement.GetProperty("properties").TryGetProperty("added", out _), Is.True);
        Assert.That(saved.RootElement.GetProperty("tags")[0].GetString(), Is.EqualTo("mine"));
        Assert.That(saved.RootElement.GetProperty("baseline").GetString(), Is.EqualTo("checksum@v1"));
        Assert.ThrowsAsync<CliException>(() => Save(Model, hash: hash));
    }

    [Test] public async Task Deleted_item_is_not_recreated_by_an_edit_or_migration()
    {
        Assert.ThrowsAsync<CliException>(() => Save(Edited, FileJson(Properties), Model));
        Assert.ThrowsAsync<CliException>(() => Save(Model, hash: "stale"));
        Assert.That(File.Exists(path), Is.False);
        await Save(Model);
        Assert.That(File.Exists(path), Is.True, "Ordinary creation still works.");
    }
}
