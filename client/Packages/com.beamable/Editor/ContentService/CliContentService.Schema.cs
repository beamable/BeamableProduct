using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
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
            public readonly object LoadIdentity;
            public LoadedContent(ContentObject content, string path, string fileJson, string modelProperties, object loadIdentity = null)
            {
                Content = content; Path = path; FileJson = fileJson; ModelProperties = modelProperties;
                LoadIdentity = loadIdentity ?? new object();
            }
        }

        private readonly Dictionary<string, LoadedContent> _loadedContent = new();
        private ConditionalWeakTable<ContentObject, string> _loadedObjectKeys;

        private readonly Dictionary<string, Promise> _queuedPropertyWrites = new();

        // Mirrors cli ContentPropertyEdits.UnsafeEditExitCode and StaleEditExitCode; the SDK and CLI ship together.
        internal const int UnsafeEditExitCode = 7;
        internal const int StaleEditExitCode = 8;

        /// <summary>A content save the CLI refused. <see cref="ExitCode"/> tells the refusal kinds apart.</summary>
        internal sealed class ContentSaveRejectedException : InvalidOperationException
        {
            public int ExitCode { get; }
            public ContentSaveRejectedException(string message, int exitCode) : base(message) { ExitCode = exitCode; }
        }

        /// <summary>The last Inspector edit of an item that was not saved, kept until a newer save succeeds or the user discards it.</summary>
        internal readonly struct ContentSaveFailure
        {
            public readonly string Message;
            public readonly bool CanReviewSchema;
            /// <summary>The edit this failure is about. Only a save of this edit or a newer one clears it.</summary>
            public readonly long Edit;
            public ContentSaveFailure(string message, bool canReviewSchema, long edit) { Message = message; CanReviewSchema = canReviewSchema; Edit = edit; }
        }

        private readonly Dictionary<string, ContentSaveFailure> _saveFailures = new();

        // Orders Inspector edits. A save that finishes must not clear a failure about an edit made after it started.
        private long _lastEdit;

        private readonly Dictionary<string, LocalContentManifestEntry> _deferredReloads = new();

        /// <summary>
        /// The watcher reports every save this editor makes. Reloading for it would overwrite edits made since
        /// that save, so wait while this item's writes are in flight, and skip the reload when the file is
        /// exactly the baseline our last save left. Returns false when a full reload is needed.
        /// </summary>
        private bool TryKeepEditsOverOwnSave(LocalContentManifestEntry entry, ContentObject contentObject)
        {
            var key = GetContentWriteKey(entry.FullId);
            if (!_loadedContent.TryGetValue(key, out var loaded) || loaded.Content != contentObject || loaded.Path != entry.JsonFilePath) return false;
            if (_contentWrites.HasWrites(key) || _queuedPropertyWrites.ContainsKey(key))
            {
                // The write in flight decides what the file means; ReloadAfterWrites runs with the latest entry.
                _deferredReloads[key] = entry;
                ApplyEntryState(entry, contentObject);
                return true;
            }
            if (ReadContentText(entry.JsonFilePath) != loaded.FileJson) return false;
            ApplyEntryState(entry, contentObject);
            return true;
        }

        private void ApplyEntryState(LocalContentManifestEntry entry, ContentObject contentObject)
        {
            contentObject.Tags = entry.Tags.ToArray();
            _contentIdTagsCache[entry.FullId] = entry.Tags.ToList();
            contentObject.ContentStatus = entry.StatusEnum;
            contentObject.IsInConflict = entry.IsInConflict;
        }

        /// <summary>Applies a watcher reload that arrived while this item was being written.</summary>
        private void ReloadAfterWrites(string key)
        {
            if (!_deferredReloads.TryGetValue(key, out var deferred)) return;
            // A later completion retries while writes for this item are still running.
            if (_contentWrites.HasWrites(key) || _queuedPropertyWrites.ContainsKey(key)) return;
            _deferredReloads.Remove(key);
            if (GetContentWriteKey(deferred.FullId) != key || !EntriesCache.TryGetValue(deferred.FullId, out var current)) return;
            CacheScriptableContent(current);
            ManifestChangedCount++;
        }

        /// <summary>
        /// Another writer changed fields the model shows around our save. Show the file now: edits queued behind
        /// this save were made against the old values, so the new load refuses them instead of overwriting unseen.
        /// </summary>
        private void ShowOutsideChange(string contentId, string key)
        {
            if (!EntriesCache.TryGetValue(contentId, out var entry) || !_contentScriptableCache.TryGetValue(contentId, out var content) || content == null) return;
            var hadPendingEdit = content.HasPendingEditorChangeNotification;
            try { LoadContentObject(entry, content); }
            catch (Exception error)
            {
                // The baseline stays at our previous load, so a later edit is still checked against the file.
                Debug.LogException(error);
                return;
            }
            if (hadPendingEdit)
            {
                RecordDiscardedEdit(contentId, key, "Content changed on disk while your edit was being saved. Your latest change was not saved; the Inspector now shows the file.");
            }
            ManifestChangedCount++;
        }

        /// <summary>A reload replaced an Inspector edit that was still waiting to save.</summary>
        private void RecordDiscardedEdit(string contentId, string key, string message)
        {
            // The lost edit is newer than any save in flight, so those saves must not clear this.
            _saveFailures[key] = new ContentSaveFailure(message, false, ++_lastEdit);
            Debug.LogError($"Content edit was not saved. id=[{contentId}] {message}");
        }

        private static string ReadContentText(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        private static string ModelPropertiesOf(string contentId, Type type, string fileJson)
        {
            try { return SerializeMigration(contentId, type, fileJson); }
            catch (Exception) { return null; }
        }

        internal bool TryGetSaveFailure(string contentId, out ContentSaveFailure failure)
        {
            failure = default;
            return !string.IsNullOrEmpty(contentId) && _saveFailures.TryGetValue(GetContentWriteKey(contentId), out failure);
        }

        private void RecordSaveFailure(string contentId, string writeKey, long edit, Exception error)
        {
            if (writeKey == null)
            {
                Debug.LogException(error);
                return;
            }
            // Offer the review for this refusal regardless of the notice preference or a recorded difference:
            // the review window computes its own preview.
            var canReview = error is ContentSaveRejectedException rejected && rejected.ExitCode == UnsafeEditExitCode;
            _saveFailures[writeKey] = new ContentSaveFailure(error.Message, canReview, edit);
            Debug.LogError($"Content edit was not saved. id=[{contentId}] {error.Message}");
            ManifestChangedCount++;
        }

        private void ClearSaveFailure(string writeKey, long edit)
        {
            if (writeKey != null && _saveFailures.TryGetValue(writeKey, out var failure) && failure.Edit <= edit)
            {
                _saveFailures.Remove(writeKey);
                ManifestChangedCount++;
            }
        }

        /// <summary>Drops a refused edit by reloading the item from its file.</summary>
        internal void DiscardRejectedEdit(string contentId)
        {
            var key = GetContentWriteKey(contentId);
            if (_contentWrites.HasWrites(key) || _queuedPropertyWrites.ContainsKey(key))
                throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then discard.");
            _saveFailures.Remove(key);
            if (EntriesCache.TryGetValue(contentId, out var entry) && entry.StatusEnum != ContentStatus.Deleted &&
                _contentScriptableCache.TryGetValue(contentId, out var content) && content != null)
                LoadContentObject(entry, content);
            ManifestChangedCount++;
        }

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
                // A reload can reuse the ScriptableObject. Only our completed saves may advance
                // the baseline of an already queued edit; a reload requires a fresh edit.
                if (!_loadedContent.TryGetValue(key, out var current) || current.Content != loaded.Content ||
                    !ReferenceEquals(current.LoadIdentity, loaded.LoadIdentity))
                    throw new InvalidOperationException("Content was reloaded because it changed on disk. Check the item, then edit it again.");
                if (current.ModelProperties == properties) return;
                await WriteContentProperties(contentId, properties, current);
                if (scope == GetContentScopeKey() && CanWriteContent(contentId) && File.Exists(current.Path) &&
                    _loadedContent.TryGetValue(key, out var active) && ReferenceEquals(active, current))
                {
                    // Re-reading can also pick up a write made by someone else after ours. Only a file whose
                    // model fields are exactly what we sent may become the baseline for edits queued behind us.
                    var written = ReadContentText(current.Path);
                    if (written != null && ModelPropertiesOf(contentId, current.Content.GetType(), written) == properties)
                        _loadedContent[key] = new LoadedContent(current.Content, current.Path, written, properties, current.LoadIdentity);
                    else
                        ShowOutsideChange(contentId, key);
                }
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
            if (!TryGetContentObject(contentId, out var content) || content == null)
                throw new InvalidOperationException("Compile the matching local C# content type before reviewing JSON changes.");
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
                throw new InvalidOperationException("Content or model changed after the JSON preview. Review this item again.");
            // The reload below replaces the Inspector values, so keep edits disabled until it finishes. This skips
            // FinishActionProgress on purpose: its full content reload is far heavier than this one-item write.
            BeginActionProgress("Apply JSON Changes", "Applying JSON changes...", false);
            try
            {
                await _contentWrites.Run(GetContentWriteKey(preview.ContentId), () =>
                    WriteContentProperties(preview.ContentId, preview.ProposedProperties, expectedFileHash: preview.FileHash));
                // The write landed. After a realm or namespace change nothing from the old scope is on screen to reload.
                if (preview.Scope != GetContentScopeKey()) return;
                var key = GetContentWriteKey(preview.ContentId);
                _lastSavedPropertiesCache.Remove(key);
                _saveFailures.Remove(key);
                // Edits can still arrive without the Inspector fields, for example through Undo or a script.
                var hadPendingEdit = content.HasPendingEditorChangeNotification;
                // Reload from the actual saved file; the watcher owns authoritative change status.
                try { LoadContentObject(entry, content); }
                catch (Exception error)
                {
                    // The file is already migrated and the watcher reloads it. Do not report the apply as failed.
                    Debug.LogException(error);
                }
                if (hadPendingEdit)
                {
                    RecordDiscardedEdit(preview.ContentId, key, "JSON changes were applied while your edit was waiting to save. Your edit was not saved; the Inspector now shows the file.");
                }
                ManifestChangedCount++;
            }
            finally
            {
                CurrentProgress.IsActive = false;
                ProgressUpdateVersion++;
            }
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
