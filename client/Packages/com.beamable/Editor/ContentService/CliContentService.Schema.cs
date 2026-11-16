using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
    public partial class CliContentService
    {
        private sealed class LoadedContent
        {
            public readonly ContentObject Content;
            public readonly string Path, FileJson, ModelProperties;
            public LoadedContent(ContentObject content, string path, string fileJson, string modelProperties)
            { Content = content; Path = path; FileJson = fileJson; ModelProperties = modelProperties; }
        }

        private readonly Dictionary<string, LoadedContent> _loadedContent = new();
        private ConditionalWeakTable<ContentObject, string> _loadedObjectKeys;

        private readonly Dictionary<string, Promise> _queuedPropertyWrites = new();

        private async Promise WriteLoadedContentProperties(string contentId, string properties, LoadedContent loaded)
        {
            var scope = GetContentScopeKey();
            var key = GetContentWriteKey(contentId);
            _queuedPropertyWrites.TryGetValue(key, out var previous);
            var settled = new Promise();
            _queuedPropertyWrites[key] = settled;
            try
            {
                if (previous != null) await previous;
                if (scope != GetContentScopeKey()) throw new InvalidOperationException("Realm or namespace changed before this edit was saved.");
                if (!CanWriteContent(contentId)) return;
                if (!_loadedContent.TryGetValue(key, out var current) || current.Content != loaded.Content)
                    throw new InvalidOperationException("Content was reloaded. Select the current item before editing.");
                if (current.ModelProperties == properties) return;
                await WriteContentProperties(contentId, properties, current);
                if (scope == GetContentScopeKey() && CanWriteContent(contentId) && File.Exists(current.Path) &&
                    _loadedContent.TryGetValue(key, out var active) && ReferenceEquals(active, current))
                    _loadedContent[key] = new LoadedContent(current.Content, current.Path, File.ReadAllText(current.Path), properties);
            }
            finally
            {
                if (_queuedPropertyWrites.TryGetValue(key, out var last) && ReferenceEquals(last, settled))
                    _queuedPropertyWrites.Remove(key);
                // A failed submission retains its baseline; a queued later edit can still retry safely.
                settled.CompleteSuccess();
            }
        }

        // An immutable, per-item review model. Presentation is deliberately separate from applying it.
        internal sealed class SchemaMigrationPreview
        {
            public string ContentId { get; }
            public string Scope { get; }
            public string OriginalJson { get; }
            public string ProposedProperties { get; }
            internal string FileHash { get; }
            internal string Path { get; }
            internal Type ModelType { get; }
            internal SchemaMigrationPreview(string id, string scope, string path, Type type, string original, string proposed)
            {
                ContentId = id; Scope = scope; Path = path; ModelType = type;
                OriginalJson = original; ProposedProperties = proposed; FileHash = HashFileText(original);
            }
        }

        internal SchemaMigrationPreview PreviewSchemaMigration(string contentId)
        {
            RequireSchemaMigrationReady(contentId);
            if (!_contentScriptableCache.TryGetValue(contentId, out var content) || content == null)
                throw new InvalidOperationException("Compile the matching content type before migrating this item.");
            var entry = EntriesCache[contentId];
            var original = File.ReadAllText(entry.JsonFilePath);
            return new SchemaMigrationPreview(contentId, GetContentScopeKey(), entry.JsonFilePath, content.GetType(),
                original, SerializeMigration(contentId, content.GetType(), original));
        }

        internal async Promise ApplySchemaMigration(SchemaMigrationPreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (preview.Scope != GetContentScopeKey()) throw new InvalidOperationException("Realm or namespace changed. Review this item again.");
            RequireSchemaMigrationReady(preview.ContentId);
            var entry = EntriesCache[preview.ContentId];
            if (entry.JsonFilePath != preview.Path || HashFileText(File.ReadAllText(preview.Path)) != preview.FileHash ||
                !_contentScriptableCache.TryGetValue(preview.ContentId, out var content) || content == null ||
                content.GetType() != preview.ModelType ||
                SerializeMigration(preview.ContentId, preview.ModelType, preview.OriginalJson) != preview.ProposedProperties)
                throw new InvalidOperationException("Content or model changed after migration preview. Review this item again.");
            await _contentWrites.Run(GetContentWriteKey(preview.ContentId), () =>
                WriteContentProperties(preview.ContentId, preview.ProposedProperties, expectedFileHash: preview.FileHash));
            if (preview.Scope != GetContentScopeKey()) return;
            _lastSavedPropertiesCache.Remove(GetContentWriteKey(preview.ContentId));
            // Reload from the actual saved file; the watcher owns authoritative change status.
            LoadContentObject(entry, content);
        }

        private void RequireSchemaMigrationReady(string contentId)
        {
            if (CurrentProgress.IsActive || isReloading || !EntriesCache.ContainsKey(contentId) || !CanWriteContent(contentId))
                throw new InvalidOperationException("Content is unavailable or an operation is running. Refresh before reviewing.");
            RequireSavedInspectorEdits(contentId);
            if (_contentWrites.HasWrites(GetContentWriteKey(contentId)))
                throw new InvalidOperationException("Content edits are still being saved. Wait before reviewing.");
        }

        private static string SerializeMigration(string contentId, Type type, string json)
        {
            var instance = (ContentObject)ScriptableObject.CreateInstance(type);
            try
            {
                // A failed conversion must never become a migration to default values.
                ClientContentSerializer.DeserializeContentFromCli(json, instance, contentId, out _, disableExceptions: false);
                return ClientContentSerializer.SerializeProperties(instance);
            }
            finally { instance.CancelPendingEditorChangeNotification(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static string HashFileText(string text)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
    }
}
