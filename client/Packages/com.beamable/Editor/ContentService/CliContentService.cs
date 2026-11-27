using Beamable;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Tracks a detected rename: a Created entry (new name) paired with a Deleted entry (old name).
	/// </summary>
	public struct ContentRenameInfo
	{
		public string CreatedFullId;
		public string DeletedFullId;
		public string OldName;
		public string NewName;
		public string TypeName;
	}

	/// <summary>
	/// Tracks the currently running content operation so the Content Manager can render progress inside the window.
	/// </summary>
	public class ContentOperationProgress
	{
		public bool IsActive;
		public bool HasError;
		public string Title = string.Empty;
		public string Description = string.Empty;
		public string CurrentContentName = string.Empty;
		public int ProcessedItems;
		public int TotalItems;
		public float Progress;
	}

	public partial class CliContentService : IStorageHandler<CliContentService>, ILoadWithContext, IBeamableDisposableOrder
	{
		private const string SYNC_OPERATION_TITLE = "Sync Contents";
		private const string SYNC_OPERATION_SUCCESS_BASE_MESSAGE = "{0} sync complete. {1}/{2} items synced.";
		private const string SYNC_OPERATION_ERROR_BASE_MESSAGE = "Error when syncing content {0}. Error message: {1}";
		private const string PUBLISH_OPERATION_TITLE = "Publish Contents";
		private const string PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE = "{0} published. {1}/{2} items published.";
		private const string ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE = "Error when publishing content {0}. Discarding publishes changes. Error message: {1}";
		// Mirrors ContentPsCommandEvent.EVT_TYPE_FullRebuild in the CLI contract.
		// The generated Unity contract exposes EventType but not its constants.
		private const int CONTENT_PS_EVENT_TYPE_FULL_REBUILD = 0;

		public string manifestIdOverride;
		
		private StorageHandle<CliContentService> _handle;
		private readonly BeamCommands _cli;
		private ContentPsWrapper _contentWatcher;
		private readonly BeamEditorContext _beamContext;
		private readonly IDependencyProvider _provider;
		private Dictionary<string, List<LocalContentManifestEntry>> _typeContentCache = new();
		private Dictionary<ContentStatus, List<LocalContentManifestEntry>> _statusContentCache = new();
		private Dictionary<string, LocalContentManifestEntry> _conflictedContentCache = new();
		private readonly ContentTypeReflectionCache _contentTypeReflectionCache;
		private Dictionary<string, LocalContentManifestEntry> _invalidContents = new();
		private readonly Dictionary<string, (SchemaDifference Difference, string Reference)> _schemaDifferences = new();
		private string _schemaNoticeCachePath = Path.Combine("Library", "BeamableEditor", "ContentSchemaNotices.json");
		private bool _schemaNoticesLoaded;
		private readonly Dictionary<string, List<string>> _contentIdTagsCache = new();
		private List<string> _tagsCache = new();
		private int syncedContents;
		private int publishedContents;
		private sealed class ContentSaveSnapshot
		{
			public string PropertiesJson;
			public bool IsSaved;
		}

		private readonly Dictionary<string, ContentSaveSnapshot> _lastSavedPropertiesCache = new();
		private readonly Dictionary<string, ContentSaveSnapshot> _pendingTagSaves = new();
		private readonly ContentWriteTracker _contentWrites = new();
		private readonly Dictionary<string, ContentRenameInfo> _renameRegistry = new();
		private readonly Dictionary<string, ContentRenameInfo> _explicitRenameTracking = new();
		private static readonly string RenameCachePath =
			Path.Combine("Library", "BeamableEditor", "ContentRenameCache.json");
		private bool _showUnityModalProgress;
		
		[NonSerialized]
		public bool isReloading;
		public List<string> availableManifestIds;
		public int ContentMetadataVersion { get; private set; }

		/// <summary>
		/// True once the most recent <see cref="Reload"/> has finished fetching the realm's remote
		/// manifest list. Until this is true the realm's emptiness is unknown (still loading).
		/// </summary>
		public bool RemoteManifestsLoaded { get; private set; }
		/// <summary>
		/// True when the last remote-manifest fetch errored. Used so the Content Manager does not
		/// misread a transient failure as "this realm is empty".
		/// </summary>
		public bool RemoteManifestsErrored { get; private set; }
		/// <summary>
		/// Number of remote manifests on the realm as of the last successful fetch. Zero means the
		/// realm has never had content published — the signal used to offer the opt-in default
		/// content import (see DefaultContentImporter).
		/// </summary>
		public int RemoteManifestCount { get; private set; }

		public Dictionary<string, LocalContentManifestEntry> EntriesCache { get; }

		private readonly Dictionary<string, ContentObject> _contentScriptableCache;
		private readonly Dictionary<string, string> _loadErrors = new();

		internal bool TryGetLoadError(string contentId, out string message)
		{
			message = null;
			return _loadErrors.Count > 0 && _loadErrors.TryGetValue(GetContentWriteKey(contentId), out message);
		}

		private void ForgetDeletedItem(string contentId)
		{
			var key = GetContentWriteKey(contentId);
			_loadErrors.Remove(key);
			_deferredReloads.Remove(key);
			_loadedContent.Remove(key);
			_lastSavedPropertiesCache.Remove(key);
			_saveFailures.Remove(key);
		}

		public BeamContentPsProgressMessage LatestProgressUpdate;
		/// <summary>
		/// Current publish or sync progress state consumed by the Content Manager IMGUI view.
		/// </summary>
		public ContentOperationProgress CurrentProgress { get; } = new();
		/// <summary>
		/// Monotonic value incremented when <see cref="CurrentProgress"/> changes so the Content Manager can repaint on demand.
		/// </summary>
		public int ProgressUpdateVersion { get; private set; }
		private readonly ContentConfiguration _contentConfiguration;

		private ValidationContext ValidationContext => _provider.GetService<ValidationContext>();
		public int ManifestChangedCount { get; private set; }

		public bool HasConflictedContent => _conflictedContentCache.Count > 0;

		public bool HasInvalidContent => _invalidContents.Count > 0;

		internal SchemaDifference GetSchemaDifference(string contentId)
		{
			if (!_contentConfiguration.validateSchemaDifference || string.IsNullOrEmpty(contentId)) return SchemaDifference.None;
			LoadSchemaNotices();
			return _schemaDifferences.TryGetValue(GetContentWriteKey(contentId), out var warning)
				? warning.Difference : SchemaDifference.None;
		}

		internal void UpdateSchemaDifference(LocalContentManifestEntry entry, SchemaDifference difference)
		{
			if (entry.StatusEnum == ContentStatus.Deleted) { RemoveSchemaNotice(entry.FullId); return; }
			// The preference controls presentation only. Evidence always follows the current file.
			LoadSchemaNotices();
			var key = GetContentWriteKey(entry.FullId);
			if (difference == SchemaDifference.None) { RemoveSchemaNotice(entry.FullId); return; }
			if (_schemaDifferences.TryGetValue(key, out var previous) && previous.Difference == difference && previous.Reference == entry.ReferenceManifestUid) return;
			_schemaDifferences[key] = (difference, entry.ReferenceManifestUid);
			ManifestChangedCount++;
			SaveSchemaNotices();
		}

		[Serializable]
		private class SchemaNoticeData
		{
			public SchemaNoticeEntry[] Entries;
		}

		[Serializable]
		private class SchemaNoticeEntry
		{
			public string Key;
			public SchemaDifference Difference;
			public string Reference;
		}

		private void LoadSchemaNotices()
		{
			if (_schemaNoticesLoaded) return;
			_schemaNoticesLoaded = true;
			try
			{
				if (!File.Exists(_schemaNoticeCachePath)) return;
				var data = JsonUtility.FromJson<SchemaNoticeData>(File.ReadAllText(_schemaNoticeCachePath));
				foreach (var entry in data?.Entries ?? Array.Empty<SchemaNoticeEntry>())
					if (entry != null && !string.IsNullOrEmpty(entry.Key))
						_schemaDifferences[entry.Key] = (entry.Difference, entry.Reference);
			}
			catch (Exception error) { Debug.LogWarning($"Could not load content schema notices: {error.Message}"); }
		}

		private void SaveSchemaNotices()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_schemaNoticeCachePath));
				var data = new SchemaNoticeData { Entries = _schemaDifferences.Select(pair => new SchemaNoticeEntry
					{ Key = pair.Key, Difference = pair.Value.Difference, Reference = pair.Value.Reference }).ToArray() };
				File.WriteAllText(_schemaNoticeCachePath, JsonUtility.ToJson(data));
			}
			catch (Exception error) { Debug.LogWarning($"Could not save content schema notices: {error.Message}"); }
		}

		private void RemoveSchemaNotice(string contentId)
		{
			LoadSchemaNotices();
			if (!_schemaDifferences.Remove(GetContentWriteKey(contentId))) return;
			ManifestChangedCount++;
			SaveSchemaNotices();
		}

		private void PruneSchemaNotices(LocalContentManifest manifest)
		{
			var realm = _beamContext.BeamCli.CurrentRealm;
			if (manifest == null || manifest.OwnerCid != realm.Cid || manifest.OwnerPid != realm.Pid ||
			    manifest.ManifestId != (GetSelectedManifestIdCliOption() ?? "global")) return;
			LoadSchemaNotices();
			var prefix = GetContentScopeKey() + "/";
			var present = new HashSet<string>(manifest.Entries.Select(entry => prefix + entry.FullId));
			var removed = _schemaDifferences.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal) && !present.Contains(key)).ToArray();
			foreach (var key in removed) _schemaDifferences.Remove(key);
			if (removed.Length > 0) SaveSchemaNotices();
		}

		public bool HasChangedContents
		{
			get
			{
				bool hasModified = GetAllContentFromStatus(ContentStatus.Modified).Count > 0;
				bool hasDeleted = GetAllContentFromStatus(ContentStatus.Deleted).Count > 0;
				bool hasCreated = GetAllContentFromStatus(ContentStatus.Created).Count > 0;
				return hasModified || hasDeleted || hasCreated;
			}
		}

		public bool TryGetRenameInfo(string fullId, out ContentRenameInfo info) =>
			_renameRegistry.TryGetValue(fullId, out info);

		public List<ContentRenameInfo> GetAllRenames() =>
			_renameRegistry.Values
				.GroupBy(r => r.CreatedFullId)
				.Select(g => g.First())
				.ToList();

		public Dictionary<string, List<LocalContentManifestEntry>> TypeContentCache => _typeContentCache;

		public List<string> TagsCache => _tagsCache;

		public CliContentService(BeamCommands cli, BeamEditorContext beamContext, IDependencyProvider provider, ContentConfiguration contentConfiguration)
		{
			_cli = cli;
			_beamContext = beamContext;
			_provider = provider;
			EntriesCache = new();
			_contentScriptableCache = new();
			ContentObject.ValidationContext = ValidationContext;
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			_contentConfiguration = contentConfiguration;
			_observedLocalContentMode = contentConfiguration.EnableLocalContentInEditor;
			EditorApplication.update += CheckLocalContentMode;
			LoadRenameCache();
			_ = Reload();
		}

		internal string GetContentScopeKey()
		{
			var realm = _beamContext.BeamCli.CurrentRealm;
			var manifestId = GetSelectedManifestIdCliOption() ?? "global";
			return $"{realm.Cid}/{realm.Pid}/{manifestId}";
		}

		private string GetContentWriteKey(string contentId) => GetContentWriteKey(GetContentScopeKey(), contentId);
		private static string GetContentWriteKey(string scope, string contentId) => $"{scope}/{contentId}";

		private bool CanWriteContent(string contentId)
		{
			return !_contentWrites.IsDeleting(GetContentWriteKey(contentId)) &&
			       (!EntriesCache.TryGetValue(contentId, out var entry) || entry.StatusEnum != ContentStatus.Deleted);
		}

		public void SaveContent(ContentObject selectedContentObject)
		{
			// Capture the key now: a failure must stay with the realm and namespace the edit was made in.
			var contentId = selectedContentObject != null ? selectedContentObject.Id : null;
			var writeKey = string.IsNullOrEmpty(contentId) ? null : GetContentWriteKey(contentId);
			var edit = ++_lastEdit;
			var loadIdentity = writeKey != null && _loadedContent.TryGetValue(writeKey, out var loaded) &&
			                   loaded.Content == selectedContentObject ? loaded.LoadIdentity : null;
			SaveContentAsync(selectedContentObject)
				.Then(_ => ClearSaveFailure(writeKey, edit))
				.Error(error => RecordSaveFailure(contentId, writeKey, edit, loadIdentity, error));
		}

		/// <summary>
		/// Completes after the properties save and any required tag update finish.
		/// Tombstoning an object prevents follow-up writes, but does not cancel a command already running.
		/// </summary>
		private async Promise SaveContentAsync(ContentObject selectedContentObject)
		{
			if (selectedContentObject == null || selectedContentObject.ContentStatus == ContentStatus.Deleted)
			{
				return;
			}

			string contentId = selectedContentObject.Id;
			if (TryGetLoadError(contentId, out var loadError)) throw new InvalidOperationException(loadError);
			string propertiesJson = ClientContentSerializer.SerializeProperties(selectedContentObject);
			_loadedContent.TryGetValue(GetContentWriteKey(contentId), out var loaded);
			if ((_displayedLoads != null && _displayedLoads.TryGetValue(selectedContentObject, out var displayed) &&
				(displayed.Key != GetContentWriteKey(contentId) || loaded == null)) ||
				(loaded != null && loaded.Content != selectedContentObject)) throw new InvalidOperationException("Content was reloaded. Select the current item before editing.");
			// Keep this edit's tags, but discard them if a newer edit supersedes this save.
			string[] tags = (selectedContentObject.Tags ?? Array.Empty<string>()).ToArray();
			string scopeKey = GetContentScopeKey();
			string writeKey = GetContentWriteKey(contentId);
			if (!CanWriteContent(contentId)) return;

			// This prevents save storms when the debounce fires for rapid-fire edits where
			// the final serialised value is the same as what was already written.
			if (!_contentWrites.HasWrites(writeKey) && _lastSavedPropertiesCache.TryGetValue(writeKey, out var lastSave)
			    && lastSave.IsSaved && lastSave.PropertiesJson == propertiesJson &&
				_contentIdTagsCache.TryGetValue(contentId, out var savedTags) && savedTags.SequenceEqual(tags))
			{
				return;
			}
			var snapshot = new ContentSaveSnapshot { PropertiesJson = propertiesJson };
			// Overlapping commands can finish out of order. Neither save may claim a known disk snapshot.
			if (_contentWrites.HasWrites(writeKey)) _lastSavedPropertiesCache.Remove(writeKey);
			else _lastSavedPropertiesCache[writeKey] = snapshot;

			bool hasValidationError = selectedContentObject.HasValidationErrors(GetValidationContext(), out List<string> _);
			UpdateContentValidationStatus(contentId, hasValidationError);
			_pendingTagSaves[writeKey] = snapshot;
			try
			{
				if (loaded == null || loaded.ModelProperties != propertiesJson || _contentWrites.HasWrites(writeKey))
					await SaveContentPropertiesAsync(contentId, propertiesJson, loaded: loaded);

				// The properties command may finish after deletion has tombstoned this object.
				if (selectedContentObject == null || selectedContentObject.ContentStatus == ContentStatus.Deleted)
				{
					return;
				}

				// A continuation must not send its tags into a newly selected realm or manifest.
				if (scopeKey != GetContentScopeKey())
				{
					return;
				}

				// Direct tag edits and newer saves own the tags now; an older properties completion must not overwrite them.
				if (!_pendingTagSaves.TryGetValue(writeKey, out var pending) || !ReferenceEquals(pending, snapshot)) return;
				if (loaded == null || !_contentIdTagsCache.TryGetValue(contentId, out var currentTags) || !currentTags.SequenceEqual(tags))
					await SetContentTagsAsync(contentId, tags);
				if (selectedContentObject != null && selectedContentObject.ContentStatus != ContentStatus.Deleted &&
				    scopeKey == GetContentScopeKey() && CanWriteContent(contentId) &&
				    _lastSavedPropertiesCache.TryGetValue(writeKey, out var currentSave) && ReferenceEquals(currentSave, snapshot))
				{
					snapshot.IsSaved = true;
				}
			}
			finally
			{
				if (_pendingTagSaves.TryGetValue(writeKey, out var pending) && ReferenceEquals(pending, snapshot))
					_pendingTagSaves.Remove(writeKey);
			}
		}

		public void SaveContent(string contentId, string contentPropertiesJson, Action onCompleted = null)
		{
			_lastSavedPropertiesCache.Remove(GetContentWriteKey(contentId));
			SaveContentPropertiesAsync(contentId, contentPropertiesJson, onCompleted).Error(Debug.LogException);
		}

		private Promise SaveContentPropertiesAsync(string contentId, string contentPropertiesJson, Action onCompleted = null, LoadedContent loaded = null)
		{
			if (!CanWriteContent(contentId)) return Promise.Success;
			var key = GetContentWriteKey(contentId);
			return RunWatchedWrite(key, async () =>
			{
				if (loaded == null) await WriteContentProperties(contentId, contentPropertiesJson);
				else await WriteLoadedContentProperties(contentId, contentPropertiesJson, loaded);
				onCompleted?.Invoke();
			});
		}

		private async Promise RunWatchedWrite(string key, Func<Promise> write)
		{
			try { await _contentWrites.Run(key, write); }
			finally { ReloadAfterWrites(key); }
		}

		private async Promise WriteContentProperties(string contentId, string contentPropertiesJson, LoadedContent loaded = null, string expectedFileHash = null)
		{
			var saveCommand = _cli.ContentSave(new ContentSaveArgs()
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				contentIds = new[] {contentId},
				contentProperties = new[] {contentPropertiesJson},
				originalFiles = loaded == null ? null : new[] { Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(loaded.FileJson)) },
				modelProperties = loaded == null ? null : new[] { loaded.ModelProperties },
				expectedFileHashes = expectedFileHash == null ? null : new[] { expectedFileHash }
			});
			Exception reportedError = null;
			saveCommand.OnError(report => reportedError ??= new ContentSaveRejectedException(report.data.message, report.data.exitCode));
			await saveCommand.Run();
			if (reportedError != null) throw reportedError;
		}
		
		public async Task<BeamContentSnapshotListResult> GetContentSnapshots()
		{
			TaskCompletionSource<BeamContentSnapshotListResult> taskWaiter = new TaskCompletionSource<BeamContentSnapshotListResult>();
			var snapshotListWrapper = _cli.ContentSnapshotList(new ContentSnapshotListArgs()
			{
				manifestId = GetSelectedManifestIdCliOption()
			});
			snapshotListWrapper.OnStreamContentSnapshotListResult(report =>
			{
				taskWaiter.SetResult(report.data);
			});
			await snapshotListWrapper.Run();
			return await taskWaiter.Task;
		}

		public async Promise RestoreSnapshot(string snapshotPath, bool isAdditiveRestore, bool deleteAfterRestore)
		{
			// Disable watcher when doing a restore
			await StopContentWatcher();

			var restoreWrapper = _cli.ContentRestore(new ContentRestoreArgs() {manifestId = GetSelectedManifestIdCliOption(), name = snapshotPath, deleteAfterRestore = deleteAfterRestore, additiveRestore = isAdditiveRestore});
			
			await restoreWrapper.Run();

			// Re-enable watcher
			await Reload();
		}

		public async Promise TakeSnapshot(string snapshotName, bool isLocal)
		{
			// Disable watcher when creating a snapshot to prevent changes on files during snapshot
			await StopContentWatcher();

			var snapshotWrapper = _cli.ContentSnapshot(new ContentSnapshotArgs()
			{
				manifestId = GetSelectedManifestIdCliOption(), name = snapshotName, snapshotType = isLocal ? ContentSnapshotType.Local : ContentSnapshotType.Shared
			});
			
			await snapshotWrapper.Run();
			
			// Re-enable watcher
			await Reload();
		}

		public void TempDisableWatcher(Action withoutWatcher)
		{
			var _ = TempDisableWatcher(() =>
			{
				withoutWatcher?.Invoke();
				return Promise.Success;
			});
		}
		public async Promise TempDisableWatcher(Func<Promise> withoutWatcher)
		{
			try
			{
				await StopContentWatcher();

				await withoutWatcher();
			}
			finally
			{
				var _ = Reload();
			}
		}

		public async Promise SyncContentsWithProgress(bool syncModified,
		                                           bool syncCreated,
		                                           bool syncConflicted,
		                                           bool syncDeleted,
		                                           string filter = null,
		                                           ContentFilterType filterType = default,
		                                           bool showUnityModalProgress = false)
		{

			syncedContents = 0;
			BeginActionProgress(SYNC_OPERATION_TITLE, "Synchronizing contents...", showUnityModalProgress);
			try
			{
				await StopContentWatcher();
				var contentSyncCommand = _cli.ContentSync(new ContentSyncArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					syncModified = syncModified,
					syncConflicts = syncConflicted,
					syncCreated = syncCreated,
					syncDeleted = syncDeleted,
					filter = filter,
					filterType = filterType,
					downloadMaxParallelCount = GetEditorSyncDownloadMaxParallelCountCliOption(),
					
				});

				
				contentSyncCommand.OnProgressStreamContentProgressUpdateData(reportData =>
				{
					HandleProgressUpdate(reportData.data, SYNC_OPERATION_TITLE, SYNC_OPERATION_SUCCESS_BASE_MESSAGE,
					                     SYNC_OPERATION_ERROR_BASE_MESSAGE, ref syncedContents);
				});
				await contentSyncCommand.Run();
			}
			finally
			{
				FinishActionProgress();
			}
		}

		public async Task SyncContents(bool syncModified = true,
		                               bool syncCreated = true,
		                               bool syncConflicted = true,
		                               bool syncDeleted = true,
		                               string filter = null,
		                               ContentFilterType filterType = default)
		{
			var contentSyncCommand = _cli.ContentSync(new ContentSyncArgs()
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				syncModified = syncModified,
				syncConflicts = syncConflicted,
				syncCreated = syncCreated,
				syncDeleted = syncDeleted,
				filter = filter,
				filterType = filterType,
				downloadMaxParallelCount = GetEditorSyncDownloadMaxParallelCountCliOption(),
			});
			
			await contentSyncCommand.Run();
		}

		public void SetContentTags(string contentId, string[] tags)
		{
			SetContentTagsAsync(contentId, tags).Error(Debug.LogException);
		}

		/// <summary>
		/// Awaits the tag command, including direct tag edits that do not save properties first.
		/// </summary>
		private Promise SetContentTagsAsync(string contentId, string[] tags)
		{
			if (!CanWriteContent(contentId)) return Promise.Success;
			var key = GetContentWriteKey(contentId);
			_pendingTagSaves.Remove(key);
			if (_contentWrites.HasWrites(key)) _lastSavedPropertiesCache.Remove(key);
			return RunWatchedWrite(key, () => WriteContentTags(contentId, tags));
		}

		private async Promise WriteContentTags(string contentId, string[] tags)
		{
			var scope = GetContentScopeKey();
			tags = (tags ?? Array.Empty<string>()).ToArray();
			if (EntriesCache.TryGetValue(contentId, out var entry))
			{
				entry.Tags = tags;
				EntriesCache[contentId] = entry;
			}

			bool clearTags = tags.Length == 0;
			var setContentTagCommand = _cli.ContentTagSet(new ContentTagSetArgs()
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				filterType = ContentFilterType.ExactIds,
				filter = contentId,
				clear = clearTags,
				tag = clearTags ? "<none>" : string.Join(",", tags)
			});
			Exception reportedError = null;
			setContentTagCommand.OnError(report => reportedError ??= new InvalidOperationException(report.data.message));
			await setContentTagCommand.Run();
			if (reportedError != null) throw reportedError;
			if (scope != GetContentScopeKey() || !CanWriteContent(contentId)) return;
			_contentIdTagsCache[contentId] = tags.ToList();
			if (_loadedContent.TryGetValue(GetContentWriteKey(contentId), out var loaded) &&
			    EntriesCache.TryGetValue(contentId, out entry) && loaded.Path == entry.JsonFilePath)
			{
				var written = ReadContentText(loaded.Path);
				if (written == loaded.WatcherFileJson) return;
				var model = ReadSavedModel(contentId, loaded.Content.GetType(), written);
				if (model.Properties == loaded.ModelProperties && model.Version == loaded.Content.Version)
				{
					loaded.WatcherFileJson = written;
					UpdateSchemaDifference(entry, model.Difference);
				}
			}
		}

		internal async Promise SaveNewContent(ContentObject content)
		{
			var key = GetContentWriteKey(content.Id);
			if (EntriesCache.ContainsKey(content.Id) || _contentWrites.HasWrites(key) ||
			    (_lastSavedPropertiesCache.TryGetValue(key, out var previous) && previous.IsSaved))
				throw new InvalidOperationException($"Content '{content.Id}' already exists or is being created.");
			await SaveContentAsync(content);
		}

		public bool DuplicateContent(LocalContentManifestEntry entry, out ContentObject duplicatedObject)
		{
			if (!TryMakeDuplicate(entry, out duplicatedObject)) return false;
			SaveNewContent(duplicatedObject).Error(Debug.LogException);
			return true;
		}

		internal bool TryMakeDuplicate(LocalContentManifestEntry entry, out ContentObject duplicatedObject)
		{
			if (TryGetContentObject(entry.FullId, out var contentObject))
			{
				duplicatedObject = Object.Instantiate(contentObject);
				string baseName = $"{contentObject.ContentName}_Copy";
				int itemsWithBaseNameCount = GetContentsFromType(contentObject.GetType())
				                                            .Count(item => item.Name.StartsWith(baseName));
				duplicatedObject.SetContentName($"{baseName}{itemsWithBaseNameCount}");
				duplicatedObject.ContentStatus = ContentStatus.Created;
				duplicatedObject.Tags = entry.Tags;
				return true;
			}

			duplicatedObject = null;
			return false;
		}

		public string RenameContent(string contentId, string newName)
		{
			if (!EntriesCache.TryGetValue(contentId, out var entry))
			{
				Debug.LogError($"No Entry found with id: {contentId}");
				return contentId;
			}
			
			if(entry.Name == newName)
				return contentId;

			// Prevents the user to rename the content with spaces
			newName = newName.Replace(' ', '_');
			
			if (!ValidateNewContentName(newName))
			{
				Debug.LogError(
					$"{newName} contains invalid characters ({string.Join(", ", Path.GetInvalidFileNameChars())}");
				return contentId;
			}

			string newFullId = $"{entry.TypeName}.{newName}";
			var fullName = $"{newFullId}.json";

			string originalPath = entry.JsonFilePath;
			string fileDirectoryPath = Path.GetDirectoryName(originalPath);
			string newFileNamePath = Path.Combine(fileDirectoryPath, fullName);
			if (File.Exists(newFileNamePath))
			{
				Debug.LogError($"A Content already exists with name: {newName}");
				return contentId;
			}
			
			
			// Apply local changes while CLI is updating the Data so Unity UI doesn't take long to update.
			if (entry.StatusEnum is not ContentStatus.Created)
			{
				string oldName = entry.Name;

				RemoveContentFromCache(entry);
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				AddContentToCache(entry);
				entry.Name = newName;
				entry.CurrentStatus = (int)ContentStatus.Created;
				entry.FullId = newFullId;
				AddContentToCache(entry);

				// Track rename: collapse chain if this was already the Created side of a prior rename (A→B→C becomes A→C)
				string ultimateDeletedId = contentId;
				string ultimateOldName = oldName;
				if (_explicitRenameTracking.TryGetValue(contentId, out var priorRename) && priorRename.CreatedFullId == contentId)
				{
					ultimateDeletedId = priorRename.DeletedFullId;
					ultimateOldName = priorRename.OldName;
					_explicitRenameTracking.Remove(priorRename.CreatedFullId);
					_explicitRenameTracking.Remove(priorRename.DeletedFullId);
					_renameRegistry.Remove(priorRename.CreatedFullId);
					_renameRegistry.Remove(priorRename.DeletedFullId);
				}

				// If renamed back to the original name (A→B→A), remove tracking entirely
				if (newFullId == ultimateDeletedId)
				{
					// Undo the Deleted+Created pair: remove the Deleted entry we just added and restore the original status
					RemoveContentFromCache(entry);
					entry.CurrentStatus = (int)ContentStatus.UpToDate;
					entry.FullId = newFullId;
					entry.Name = newName;
					// Also remove the stale Deleted entry we added above
					if (EntriesCache.TryGetValue(ultimateDeletedId, out var staleDeleted))
						RemoveContentFromCache(staleDeleted);
					AddContentToCache(entry);
					SaveRenameCache();
				}
				else
				{
					var renameInfo = new ContentRenameInfo
					{
						CreatedFullId = newFullId,
						DeletedFullId = ultimateDeletedId,
						OldName = ultimateOldName,
						NewName = newName,
						TypeName = entry.TypeName,
					};
					_explicitRenameTracking[newFullId] = renameInfo;
					_explicitRenameTracking[ultimateDeletedId] = renameInfo;
					_renameRegistry[newFullId] = renameInfo;
					_renameRegistry[ultimateDeletedId] = renameInfo;
					SaveRenameCache();
				}
			}
			else
			{
				RemoveContentFromCache(entry);

				// If this Created entry is the Created side of a prior rename (A→B), update tracking to A→C
				if (_explicitRenameTracking.TryGetValue(contentId, out var priorRename) && priorRename.CreatedFullId == contentId)
				{
					_explicitRenameTracking.Remove(priorRename.CreatedFullId);
					_explicitRenameTracking.Remove(priorRename.DeletedFullId);
					_renameRegistry.Remove(priorRename.CreatedFullId);
					_renameRegistry.Remove(priorRename.DeletedFullId);

					// If renamed back to the original name (A→B→A), just clear tracking
					if (newFullId != priorRename.DeletedFullId)
					{
						var renameInfo = new ContentRenameInfo
						{
							CreatedFullId = newFullId,
							DeletedFullId = priorRename.DeletedFullId,
							OldName = priorRename.OldName,
							NewName = newName,
							TypeName = entry.TypeName,
						};
						_explicitRenameTracking[newFullId] = renameInfo;
						_explicitRenameTracking[priorRename.DeletedFullId] = renameInfo;
						_renameRegistry[newFullId] = renameInfo;
						_renameRegistry[priorRename.DeletedFullId] = renameInfo;
					}
					SaveRenameCache();
				}

				entry.Name = newName;
				entry.FullId = newFullId;
				AddContentToCache(entry);
			}

			ManifestChangedCount++;

			BeamUnityFileUtils.RenameFile(originalPath, fullName);
			
			return newFullId;
		}

		/// <summary>
		/// Marks the cached content object as deleted, detaches its editor change callback,
		/// and cancels any pending change notification to prevent further autosave requests.
		/// Does not delete the content file or cancel saves already running.
		/// </summary>
		/// <param name="contentId">The full ID of the content object to tombstone.</param>
		private void TombstoneCachedContentObject(string contentId)
		{
			if (!_contentScriptableCache.TryGetValue(contentId, out var contentObject)
			    || contentObject == null)
			{
				return;
			}

			contentObject.ContentStatus = ContentStatus.Deleted;
			contentObject.OnEditorChanged = null;
			contentObject.CancelPendingEditorChangeNotification();
		}
		
		private bool ValidateNewContentName(string newName)
		{
			return Path.GetInvalidFileNameChars().All(invalidFileNameChar => !newName.Contains(invalidFileNameChar));
		}

		/// <summary>
		/// Tombstones the object immediately, waits for all its submitted writes, then removes the file.
		/// Callers must await completion before selecting the deleted placeholder or resuming the watcher.
		/// </summary>
		public Promise DeleteContent(string contentId)
		{
			if (!EntriesCache.TryGetValue(contentId, out var entry))
			{
				Debug.LogError($"No Content found with id: {contentId}");
				return Promise.Success;
			}
			
			var scopeKey = GetContentScopeKey();
			var writeKey = GetContentWriteKey(contentId);
			var path = entry.JsonFilePath;
			_contentScriptableCache.TryGetValue(contentId, out var contentObject);
			var onEditorChanged = contentObject != null ? contentObject.OnEditorChanged : null;
			TombstoneCachedContentObject(contentId);
			_pendingTagSaves.Remove(writeKey);
			_lastSavedPropertiesCache.Remove(writeKey);
			return _contentWrites.Delete(writeKey, () =>
			{
				// A realm/manifest switch must not redirect this deletion to the newly selected content.
				try
				{
					File.Delete(path);
				}
				catch
				{
					// A failed file removal must leave the surviving object editable, without reviving another scope's object.
					if (scopeKey == GetContentScopeKey() && contentObject != null &&
					    _contentScriptableCache.TryGetValue(contentId, out var cached) && ReferenceEquals(cached, contentObject))
					{
						contentObject.ContentStatus = EntriesCache.TryGetValue(contentId, out var currentEntry)
							? currentEntry.StatusEnum : entry.StatusEnum;
						contentObject.OnEditorChanged = onEditorChanged;
					}
					throw;
				}
				if (scopeKey != GetContentScopeKey()) return;

				// The watcher may have refreshed this entry while a command was finishing.
				RemoveSchemaNotice(contentId);
				ForgetDeletedItem(contentId);
				TombstoneCachedContentObject(contentId);
				if (EntriesCache.TryGetValue(contentId, out var current)) RemoveContentFromCache(current);
				ValidationContext.AllContent.Remove(contentId);
				_contentScriptableCache.Remove(contentId);
				if (entry.StatusEnum != ContentStatus.Created)
				{
					entry.CurrentStatus = (int)ContentStatus.Deleted;
					// This successful local deletion changes a previously clean copy; keep other
					// incoming/conflict metadata intact until the authoritative watcher refresh.
					if (entry.ChangeKind == (int)ContentChangeKind.UpToDate)
						entry.ChangeKind = (int)ContentChangeKind.LocalEdit;
					AddContentToCache(entry);
				}
				// Refresh selected Inspectors immediately, without waiting for the filesystem watcher.
				ManifestChangedCount++;
			});
		}

		internal event Func<string, bool> HasPendingInspectorEdits;

		private void RequireSavedInspectorEdits(string contentId)
		{
			RequireInspectorNotificationsSettled(contentId, HasPendingInspectorEdits?.GetInvocationList());
			if (_pendingTagSaves.Count > 0 && _pendingTagSaves.ContainsKey(GetContentWriteKey(contentId)))
				throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then reconcile again.");
		}

		private void RequireInspectorNotificationsSettled(string contentId, Delegate[] inspectors)
		{
			if (_contentScriptableCache.TryGetValue(contentId, out var content) && content != null && content.HasPendingEditorChangeNotification)
				throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then reconcile again.");
			if (inspectors == null) return;
			foreach (Func<string, bool> pending in inspectors)
				if (pending(contentId))
					throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then reconcile again.");
		}

		private void RequireNoRefusedInspectorEdit(string contentId, string key)
		{
			if (_saveFailures.TryGetValue(key, out var failure) && IsDisplayingRefusedEdit(contentId, key, failure, out _) && !HasPendingWrites(key))
				throw new InvalidOperationException($"The Inspector edit for {contentId} was not saved. Save or discard it in the Inspector before continuing.");
		}

		public void ResolveConflict(string contentId, bool useLocal, string reviewedScope = null, string reviewedManifestUid = null)
		{
			ReconcileContentWithProgress(contentId, useLocal, reviewedScope, reviewedManifestUid).Error(error =>
				EditorUtility.DisplayDialog("Reconciliation did not complete", error.Message, "Close"));
		}

		internal ContentResolveWrapper CreateReconciliationPreview(string contentId, string reviewedScope, string reviewedManifestUid)
		{
			if (CurrentProgress.IsActive)
				throw new InvalidOperationException("A content operation is already running. Wait for it to finish before reviewing.");
			RequireReviewedContent(contentId, reviewedScope, reviewedManifestUid);
			var key = GetContentWriteKey(contentId);
			if (_contentWrites.HasWrites(key) || _contentWrites.IsDeleting(key))
				throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then refresh the review.");
			return _cli.ContentResolve(new ContentResolveArgs
			{
				preview = true, manifestIds = GetSelectedManifestIdsCliOption(), filter = contentId,
				filterType = ContentFilterType.ExactIds, expectedManifestUid = reviewedManifestUid
			});
		}

		internal bool HasContentType(string typeName) => _contentTypeReflectionCache.TryGetType(typeName, out _);

		private void RequireReviewedContent(string contentId, string reviewedScope, string reviewedManifestUid)
		{
			if (string.IsNullOrEmpty(reviewedManifestUid) || reviewedScope != GetContentScopeKey() ||
			    !EntriesCache.TryGetValue(contentId, out var current) || current.TargetManifestUid != reviewedManifestUid)
				throw new InvalidOperationException("This item changed after it was reviewed.");
			RequireSavedInspectorEdits(contentId);
			RequireNoRefusedInspectorEdit(contentId, GetContentWriteKey(contentId));
		}

		public async Promise ReconcileContentWithProgress(string contentId, bool useLocal, string reviewedScope = null, string reviewedManifestUid = null,
			string reviewedLocalRevision = null)
		{
			if (CurrentProgress.IsActive)
				throw new InvalidOperationException("A content operation is already running. Wait for it to finish before reconciling.");
			if (!EntriesCache.TryGetValue(contentId, out var entry) || string.IsNullOrEmpty(entry.TargetManifestUid))
				throw new InvalidOperationException("Refresh content with the updated CLI before reconciling this item.");
			reviewedScope ??= GetContentScopeKey();
			reviewedManifestUid ??= entry.TargetManifestUid;
			RequireReviewedContent(contentId, reviewedScope, reviewedManifestUid);
			if (useLocal && entry.IsInConflict && IsContentInvalid(contentId))
				throw new InvalidOperationException("Fix validation errors before keeping the local version of this conflict.");
			var writeKey = GetContentWriteKey(contentId);
			var args = new ContentResolveArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(), filter = contentId,
				filterType = ContentFilterType.ExactIds, use = useLocal ? "local" : "realm",
				expectedManifestUid = reviewedManifestUid, expectedLocalRevision = reviewedLocalRevision
			};
			BeginActionProgress("Reconcile Content", "Reconciling selected content...", false);
			try
			{
				// A write can fail and leave the tracker while watcher shutdown is still pending.
				var pendingWrites = _contentWrites.WaitForWrites(writeKey);
				_ = pendingWrites.Error(_ => { }); // Observe now; the await below still propagates the failure.
				await StopContentWatcher();
				await pendingWrites;
				RequireReviewedContent(contentId, reviewedScope, reviewedManifestUid);
				var command = _cli.ContentResolve(args);
				Exception reportedError = null;
				command.OnError(report => reportedError ??= new InvalidOperationException(report.data.message));
				await command.Run();
				// Successful transport completion can still carry a rejected CLI operation.
				if (reportedError != null) throw reportedError;
			}
			finally { FinishActionProgress(reviewedScope); }
		}

		public async Promise PublishContentsWithProgress(bool showUnityModalProgress = true)
		{
			BeginActionProgress(PUBLISH_OPERATION_TITLE, "Publishing contents...", showUnityModalProgress);
			publishedContents = 0;
			
			try
			{
				var contentPublishArgs = new ContentPublishArgs
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					autoSnapshotType = _contentConfiguration.OnPublishAutoSnapshotType
				};
				if (_contentConfiguration.MaxLocalAutoSnapshot.HasValue)
				{
					contentPublishArgs.maxLocalSnapshots = _contentConfiguration.MaxLocalAutoSnapshot.Value;
				}
				var publishCommand = _cli.ContentPublish(contentPublishArgs);
				Exception reportedError = null;
				publishCommand.OnError(report => reportedError ??= new InvalidOperationException(report.data.message));
				publishCommand.OnProgressStreamContentProgressUpdateData(report =>
				{
					HandleProgressUpdate(report.data, PUBLISH_OPERATION_TITLE, PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE,
					                     ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE, ref publishedContents);
				});
				await publishCommand.Run();
				if (reportedError != null) throw reportedError;
			}
			finally
			{
				FinishActionProgress();
			}
		}

		/// <summary>
		/// Finishes the active content operation, clears any modal progress UI, and reloads local content state.
		/// </summary>
		private void FinishActionProgress(string reviewedScope = null)
		{
			if (_showUnityModalProgress)
			{
				EditorUtility.ClearProgressBar();
			}

			CurrentProgress.IsActive = false;
			ProgressUpdateVersion++;
			_ = reviewedScope == null ? Reload() : ReloadAfterReconciliation(reviewedScope);
		}

		/// <summary>
		/// Initializes progress state for a content operation. Sync can use in-window progress only, while publish can still opt into Unity's modal progress UI.
		/// </summary>
		private void BeginActionProgress(string title, string description, bool showUnityModalProgress)
		{
			_showUnityModalProgress = showUnityModalProgress;
			CurrentProgress.IsActive = true;
			CurrentProgress.Title = title;
			CurrentProgress.Description = description;
			CurrentProgress.CurrentContentName = string.Empty;
			CurrentProgress.ProcessedItems = 0;
			CurrentProgress.TotalItems = 0;
			CurrentProgress.Progress = 0;
			CurrentProgress.HasError = false;
			ProgressUpdateVersion++;

			if (_showUnityModalProgress)
			{
				EditorUtility.DisplayProgressBar(title, description, 0);
			}
		}

		/// <summary>
		/// Converts the optional editor sync concurrency setting to the CLI argument value.
		/// </summary>
		/// <remarks>
		/// Returning the default int omits the CLI argument, allowing the CLI default to apply. A configured value of 0 maps to -1 so the generated command includes an explicit unbounded setting.
		/// </remarks>
		private int GetEditorSyncDownloadMaxParallelCountCliOption()
		{
			if (_contentConfiguration.EditorSyncDownloadMaxParallelCount?.HasValue != true)
			{
				return default;
			}

			var value = _contentConfiguration.EditorSyncDownloadMaxParallelCount.Value;
			return value == 0 ? -1 : value;
		}

		public async Task PublishContents()
		{
			var contentPublishArgs = new ContentPublishArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				autoSnapshotType = _contentConfiguration.OnPublishAutoSnapshotType
			};
			if (_contentConfiguration.MaxLocalAutoSnapshot.HasValue)
			{
				contentPublishArgs.maxLocalSnapshots = _contentConfiguration.MaxLocalAutoSnapshot.Value;
			}
			var publishCommand = _cli.ContentPublish(contentPublishArgs);
			await publishCommand.Run();
		}

		internal Promise ReloadAfterReconciliation(string reviewedScope)
		{
			// Resolving one item can restart the watcher for every item in the namespace.
			if (_contentConfiguration != null && _contentConfiguration.EnableLocalContentInEditor && reviewedScope == GetContentScopeKey())
				_exitReviewReloadScope = reviewedScope;
			return Reload();
		}

		public async Promise Reload()
		{
			if (_watchersDisposed) return;
			_contentWatchRequested = true;
			Task ready = Task.CompletedTask;
			await WatcherGate.WaitAsync();
			try { if (!_watchersDisposed) ready = await StartContentWatcher(); }
			finally { WatcherGate.Release(); }
			await ready;
		}

		// Release the transition gate after startup; a slow first response must remain cancellable.
		private async Task<Task> StartContentWatcher(string activationScope = null, Func<bool> cancelled = null)
		{
			var guarded = activationScope != null;
			if ((!_contentWatchRequested && !guarded) || _watchersDisposed) return Task.CompletedTask;
			await StopLocalModeExitCheck();
			isReloading = true;
			try
			{
				// During an exit review, edits can still arrive before the first watcher response.
				var exitReviewScope = _exitReviewReloadScope;
				if (exitReviewScope == null || exitReviewScope != GetContentScopeKey() || !_contentConfiguration.EnableLocalContentInEditor)
				{
					_exitReviewReloadScope = exitReviewScope = null;
					ClearCaches();
				}
				await StopContentWatcher(preserveRequest: true);
				if ((!_contentWatchRequested && !guarded) || _watchersDisposed)
				{
					isReloading = false;
					return Task.CompletedTask;
				}

				var initialContent = new TaskCompletionSource<bool>();
				string startupCommandError = null;
				_ordinaryContentReady = guarded ? null : initialContent;
				var startupDeadline = WatcherTime + ContentStartupIdleSeconds;
				var completedItems = 0;

				_contentWatcher = _cli.ContentPs(new ContentPsArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					noAutoSync = guarded || _contentConfiguration.EnableLocalContentInEditor,
					guardLocalModeExit = guarded,
					watch = true
				});

				_contentWatcherLocalMode = !guarded && _contentConfiguration.EnableLocalContentInEditor;
				var watcher = _contentWatcher;
				void OnDataReceived(ReportDataPoint<BeamContentPsCommandEvent> report)
				{
					if (!ReferenceEquals(_contentWatcher, watcher)) return;
					if (guarded && !initialContent.Task.IsCompleted)
					{
						try { ValidateLocalModeActivation(report.data, activationScope, cancelled); }
						catch (Exception error) { initialContent.TrySetException(error); return; }
						if (!_contentWatchRequested)
						{
							initialContent.TrySetResult(true);
							return;
						}
					}
					if (initialContent.Task.IsFaulted) return;
					var currentRealm = _beamContext.BeamCli.CurrentRealm
						?? throw new InvalidOperationException("Content could not load because the editor has no active realm. Select a realm and retry.");

					bool ValidateManifest(LocalContentManifest item) =>
						item.OwnerCid == currentRealm.Cid && item.OwnerPid == currentRealm.Pid;

					var reportData = report.data;
					var isFullRebuild = reportData.EventType == CONTENT_PS_EVENT_TYPE_FULL_REBUILD;
					var changesManifest = reportData.RelevantManifestsAgainstLatest.FirstOrDefault(ValidateManifest);
					Dictionary<string, ExitReviewEdit> reviewEdits = null;
					if (isFullRebuild)
					{
						if (exitReviewScope != null && exitReviewScope == GetContentScopeKey() && changesManifest != null &&
						    exitReviewScope == $"{changesManifest.OwnerCid}/{changesManifest.OwnerPid}/{changesManifest.ManifestId}")
							reviewEdits = GetExitReviewEdits(changesManifest);
						if (exitReviewScope != null) _exitReviewReloadScope = exitReviewScope = null;
						ClearCaches();
					}
					var removeManifest = reportData.ToRemoveLocalEntries.FirstOrDefault(ValidateManifest);

					bool hasChangeManifest = changesManifest != null && changesManifest.Entries.Length > 0;
					bool hasRemoveManifest = removeManifest != null && removeManifest.Entries.Length > 0;

					if (hasChangeManifest)
					{
						foreach (var entry in changesManifest.Entries)
						{
							if (EntriesCache.TryGetValue(entry.FullId, out LocalContentManifestEntry oldEntry))
							{
								RemoveContentFromCache(oldEntry);
							}

							ExitReviewEdit edit = default;
							reviewEdits?.TryGetValue(entry.FullId, out edit);
							AddContentToCache(entry, edit);
						}
					}

					if (hasRemoveManifest)
					{
						foreach (var entry in removeManifest.Entries)
						{
							ForgetDeletedItem(entry.FullId);
							RemoveSchemaNotice(entry.FullId);
							if (EntriesCache.TryGetValue(entry.FullId, out LocalContentManifestEntry oldEntry))
							{
								RemoveContentFromCache(oldEntry);
								if (oldEntry.StatusEnum is not ContentStatus.Created)
								{
									AddContentToCache(entry);
								}
							}
							else
							{
								AddContentToCache(entry);
							}

							ValidationContext.AllContent.Remove(entry.FullId);
						}
					}

					if (isFullRebuild) PruneSchemaNotices(changesManifest);
					if (isFullRebuild || hasRemoveManifest || hasChangeManifest)
					{
						RestoreRenameRegistry();
						ManifestChangedCount++;
					}

					initialContent.TrySetResult(true);

					ValidationContext.Initialized = true;
					ComputeTags();
					ValidateAllContents();
				}

				_contentWatcher.OnStreamContentPsCommandEvent(report =>
				{
					try { OnDataReceived(report); }
					catch (Exception error) when (!guarded && !initialContent.Task.IsCompleted)
					{
						// The dispatcher logs callback exceptions. Give the waiting reload the startup failure too.
						initialContent.TrySetException(error);
					}
				});
				_contentWatcher.OnProgressStreamContentPsProgressMessage(dp =>
				{
					if (!ReferenceEquals(_contentWatcher, watcher)) return;
					LatestProgressUpdate = dp.data;
					if (!guarded && !initialContent.Task.IsCompleted && dp.data.completed > completedItems)
					{
						completedItems = dp.data.completed;
						startupDeadline = WatcherTime + ContentStartupIdleSeconds;
					}
				});
				if (guarded)
				{
					_localModeExitCheck = watcher.Command;
					watcher.OnError(report => initialContent.TrySetException(new InvalidOperationException(report.data.message)));
				}
				else
				{
					// A CLI error can precede a clean EOF. A later snapshot still counts as a successful start.
					watcher.OnError(report =>
					{
						if (!initialContent.Task.IsCompleted) startupCommandError = report.data.message;
					});
				}
				var completion = _contentWatcherCompletion = AwaitCommand(watcher.Command.Run());
				if (guarded)
				{
					_localModeExitCheckCompletion = completion;
					if (!_contentWatchRequested) return FinishReload();
				}

				RemoteManifestsLoaded = false;
				RemoteManifestsErrored = false;
				// Namespace discovery must not hold content readiness hostage to a separate remote request.
				_ = _cli.ContentListManifests(new ContentListManifestsArgs()).OnStreamContentListManifestsCommandResults(dp =>
				{
					if (!ReferenceEquals(_contentWatcher, watcher)) return;
					var manifestIds = new HashSet<string>();
					foreach (var id in dp.data.localManifests)
					{
						manifestIds.Add(id);
					}

					foreach (var id in dp.data.remoteManifests)
					{
						manifestIds.Add(id);
					}

					// A realm with zero remote manifests has never had content published to it.
					// Seeding default content is deferred to an explicit, user-initiated action
					// (see DefaultContentImporter) so we never mutate the AssetDatabase / Addressables
					// at init time. Here we only record the signal the Content Manager uses to offer
					// the opt-in import prompt.
					RemoteManifestCount = dp.data.remoteManifests.Count;
					RemoteManifestsLoaded = true;

					availableManifestIds = manifestIds.ToList();
					ContentMetadataVersion++;
				}).OnError(dp =>
				{
					if (!ReferenceEquals(_contentWatcher, watcher)) return;
					RemoteManifestsErrored = true;
					ContentMetadataVersion++;
					Debug.LogError(dp.data.message);
				}).Run().Error(error =>
				{
					if (!ReferenceEquals(_contentWatcher, watcher)) return;
					RemoteManifestsErrored = true;
					ContentMetadataVersion++;
					Debug.LogException(error);
				});

				return FinishReload();

				async Task FinishReload()
				{
					void CheckStartupDeadline()
					{
						if (!_watchersDisposed && ReferenceEquals(_contentWatcher, watcher) && WatcherTime >= startupDeadline)
							initialContent.TrySetException(new TimeoutException(ContentStartupTimeoutMessage));
					}

					if (!guarded && !initialContent.Task.IsCompleted) EditorApplication.update += CheckStartupDeadline;
					try
					{
						if (await Task.WhenAny(initialContent.Task, completion) == completion)
						{
							if (!guarded && !_preparedForAssemblyReload && !ReferenceEquals(_contentWatcher, watcher))
							{
								// EOF can precede the stop acknowledgement. Normal retirement settles this caller after shutdown.
								await initialContent.Task;
							}
							await completion;
							if (!guarded && !ReferenceEquals(_contentWatcher, watcher)) return;
							throw new InvalidOperationException(string.IsNullOrWhiteSpace(startupCommandError)
								? "Content watcher ended before its initial content response."
								: startupCommandError);
						}
						if (!await initialContent.Task)
						{
							// Shutdown finished before a snapshot. Preserve any terminal command error for this reload.
							await completion;
							return;
						}
						if (ReferenceEquals(_contentWatcher, watcher)) ManifestChangedCount++;
					}
					catch (Exception error) when (!guarded && !_watchersDisposed && ReferenceEquals(_contentWatcher, watcher))
					{
						ContentWatcherError = error.Message;
						if (initialContent.Task.IsFaulted)
						{
							// A timeout or unreadable first snapshot can leave the command running.
							isReloading = false;
							var stop = BeginContentWatcherStop(preserveRequest: true);
							_ = stop.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
						}
						throw;
					}
					finally
					{
						if (!guarded)
						{
							EditorApplication.update -= CheckStartupDeadline;
							// A command can exit before a snapshot. A later stop cannot fault an abandoned readiness task.
							initialContent.TrySetCanceled();
						}
						if (ReferenceEquals(_contentWatcher, watcher)) isReloading = false;
					}
				}
			}
			catch (Exception error)
			{
				// Setup can fail before FinishReload owns the first-snapshot wait.
				if (!guarded && !_watchersDisposed) ContentWatcherError = error.Message;
				isReloading = false;
				throw;
			}
		}

		private void ValidateAllContents()
		{
			foreach (KeyValuePair<string, ContentObject> scriptableCache in _contentScriptableCache)
			{
				ValidateForInvalidFields(scriptableCache.Value);
			}
		}

		private void ClearCaches()
		{
			LatestProgressUpdate = new BeamContentPsProgressMessage();
			ValidationContext.AllContent.Clear();
			EntriesCache.Clear();
			TypeContentCache.Clear();
			_invalidContents.Clear();
			_statusContentCache.Clear();
			_conflictedContentCache.Clear();
			_contentIdTagsCache.Clear();
			_lastSavedPropertiesCache.Clear();
			_loadedContent.Clear();
			_deferredReloads.Clear();
			_loadErrors.Clear();
			_renameRegistry.Clear();
		}

		private void RestoreRenameRegistry()
		{
			_renameRegistry.Clear();

			foreach (var kvp in _explicitRenameTracking)
			{
				if (EntriesCache.ContainsKey(kvp.Value.CreatedFullId) &&
				    EntriesCache.ContainsKey(kvp.Value.DeletedFullId))
				{
					_renameRegistry[kvp.Key] = kvp.Value;
				}
			}

			var staleKeys = _explicitRenameTracking
				.Where(kvp => !EntriesCache.ContainsKey(kvp.Value.CreatedFullId) ||
				              !EntriesCache.ContainsKey(kvp.Value.DeletedFullId))
				.Select(kvp => kvp.Key)
				.ToList();
			if (staleKeys.Count > 0)
			{
				foreach (var key in staleKeys)
					_explicitRenameTracking.Remove(key);
				SaveRenameCache();
			}
		}

		private void LoadRenameCache()
		{
			try
			{
				if (File.Exists(RenameCachePath))
				{
					var json = File.ReadAllText(RenameCachePath);
					var data = JsonUtility.FromJson<RenameCacheData>(json);
					if (data?.Entries != null)
					{
						_explicitRenameTracking.Clear();
						foreach (var e in data.Entries)
						{
							var info = new ContentRenameInfo
							{
								CreatedFullId = e.CreatedFullId,
								DeletedFullId = e.DeletedFullId,
								OldName = e.OldName,
								NewName = e.NewName,
								TypeName = e.TypeName,
							};
							_explicitRenameTracking[info.CreatedFullId] = info;
							_explicitRenameTracking[info.DeletedFullId] = info;
						}
					}
				}
			}
			catch { }
		}

		private void SaveRenameCache()
		{
			try
			{
				var dir = Path.GetDirectoryName(RenameCachePath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				var distinct = _explicitRenameTracking.Values
					.GroupBy(r => r.CreatedFullId)
					.Select(g => g.First())
					.ToArray();
				var data = new RenameCacheData
				{
					Entries = distinct.Select(r => new RenameCacheEntry
					{
						CreatedFullId = r.CreatedFullId,
						DeletedFullId = r.DeletedFullId,
						OldName = r.OldName,
						NewName = r.NewName,
						TypeName = r.TypeName,
					}).ToArray()
				};
				File.WriteAllText(RenameCachePath, JsonUtility.ToJson(data));
			}
			catch { }
		}

		[Serializable]
		private class RenameCacheData { public RenameCacheEntry[] Entries; }

		[Serializable]
		private class RenameCacheEntry
		{
			public string CreatedFullId;
			public string DeletedFullId;
			public string OldName;
			public string NewName;
			public string TypeName;
		}

		public void ReceiveStorageHandle(StorageHandle<CliContentService> handle)
		{
			_handle = handle;
		}

		public static async Promise<bool> HasLocalChanges()
		{
			var api = BeamEditorContext.Default;
			await api.InitializePromise;

			await api.CliContentService.Reload();

			return api.CliContentService.EntriesCache.Any(item => item.Value.StatusEnum is not ContentStatus
				                                                .UpToDate);
		}

		public IValidationContext GetValidationContext()
		{
			return ValidationContext;
		}

		public List<LocalContentManifestEntry> GetContentsFromType(Type contentType, bool getSubContent = false)
		{
			var contentTypeName = ContentTypeReflectionCache.GetContentTypeName(contentType);
			return GetContentFromTypeName(contentTypeName, getSubContent);
		}

		public List<LocalContentManifestEntry> GetContentFromTypeName(string contentTypeName, bool getSubContent = false)
		{
			if (getSubContent)
			{
				var contents = new List<LocalContentManifestEntry>();
				foreach (var keyValuePair in TypeContentCache.Where(keyValuePair => keyValuePair.Key.StartsWith(contentTypeName)))
				{
					contents.AddRange(keyValuePair.Value);
				}

				return contents;
			}
			if (!TypeContentCache.TryGetValue(contentTypeName, out var typeContents))
			{
				return new List<LocalContentManifestEntry>();
			}

			return new List<LocalContentManifestEntry>(typeContents);
		}

		public List<LocalContentManifestEntry> GetAllContentFromStatus(ContentStatus status)
		{
			if (!_statusContentCache.TryGetValue(status, out var contents))
			{
				return new List<LocalContentManifestEntry>();
			}
			return new List<LocalContentManifestEntry>(contents);
		}

		public List<LocalContentManifestEntry> GetAllConflictedContents()
		{
			return _conflictedContentCache.Values.ToList();
		}

		public List<LocalContentManifestEntry> GetAllInvalidContents()
		{
			return _invalidContents.Values.ToList();
		}

		private void ComputeTags()
		{
			HashSet<string> tags = new HashSet<string>();
			foreach (var keyValuePair in _contentIdTagsCache)
			{
				keyValuePair.Value.ForEach(tag => tags.Add(tag));
			}

			_tagsCache = tags.ToList();
		}

		public static async Task<string> LoadContentFileData(LocalContentManifestEntry entry)
		{
			FileStream fileStream =
				new FileStream(entry.JsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			StreamReader streamReader = new StreamReader(fileStream);

			string fileContent = await streamReader.ReadToEndAsync();
			streamReader.Close();
			fileStream.Close();
			return fileContent;
		}

		public bool IsContentInvalid(string entryId)
		{
			return _invalidContents.ContainsKey(entryId);
		}

		public void UpdateContentValidationStatus(string itemId, bool hasValidationError)
		{
			
			if (hasValidationError && EntriesCache.TryGetValue(itemId, out var entry))
			{
				_invalidContents[entry.FullId] = entry;
			}
			else
			{
				_invalidContents.Remove(itemId);
			}
		}

		public string[] GetSelectedManifestIdsCliOption()
		{
			if (string.IsNullOrEmpty(manifestIdOverride))
			{
				return null;
			}
			// perhaps if the manifestIdOverride was "global", we don't need to specify it, because that is the default anyway.
			return new string[] {manifestIdOverride};
		}

		public string GetSelectedManifestIdCliOption()
		{
			return string.IsNullOrEmpty(manifestIdOverride) ? null : manifestIdOverride;
		}

		private void AddContentToCache(LocalContentManifestEntry entry, ExitReviewEdit reviewEdit = default)
		{
			if (entry.StatusEnum is not ContentStatus.Deleted &&
			    (string.IsNullOrEmpty(entry.JsonFilePath) || !File.Exists(entry.JsonFilePath)))
			{
				return;
			}

			var entryType = _contentTypeReflectionCache.GetTypeFromId(entry.FullId);
			if (!_contentTypeReflectionCache.TryGetName(entryType, out var typeName))
			{
				//Fallback to CLI Value
				typeName = entry.TypeName;
			}

			if (!TypeContentCache.TryGetValue(typeName, out var typeContentList))
			{
				TypeContentCache[typeName] = typeContentList = new List<LocalContentManifestEntry>();;
			}
			
			// Before using the new CliContent on Unity we supported Content names with dots.
			// So before caching the content data that comes from Cli we need to fix any names that might came wrong by applying it again using the real contentTypeName
			entry.TypeName = typeName;
			entry.Name = entry.FullId.Substring(typeName.Length + 1);
			
			typeContentList.Add(entry);

			if (!_statusContentCache.TryGetValue(entry.StatusEnum, out var statusContents))
			{
				_statusContentCache[entry.StatusEnum] = statusContents = new List<LocalContentManifestEntry>();
			}
			statusContents.Add(entry);

			_conflictedContentCache.Remove(entry.FullId);
			if (entry.IsInConflict)
			{
				_conflictedContentCache.Add(entry.FullId, entry);
			}

			_contentIdTagsCache[entry.FullId] = entry.Tags.ToList();
			
			EntriesCache[entry.FullId] = entry;

			if (reviewEdit.Baseline == null && reviewEdit.LoadError == null)
				CacheScriptableContent(entry);
			else
			{
				var key = GetContentWriteKey(entry.FullId);
				var content = _contentScriptableCache[entry.FullId];
				if (reviewEdit.Baseline != null)
				{
					_loadedContent[key] = reviewEdit.Baseline;
					if (HasPendingWrites(key)) _deferredReloads[key] = entry;
				}
				else
					// A failed load left this Inspector read-only. Discard can retry the repaired file.
					_loadErrors[key] = reviewEdit.LoadError;
				ApplyEntryState(entry, content);
				ValidateForInvalidFields(content);
			}

			if (entry.StatusEnum is not ContentStatus.Deleted)
			{
				ValidationContext.AllContent[entry.FullId] = entry;
			}
		}

		private void CacheScriptableContent(LocalContentManifestEntry entry)
		{
			// In-flight writes can generate Modified events while deletion is waiting.
			// Do not let those events revive the tombstoned object or schedule another autosave.
			if (_contentWrites.IsDeleting(GetContentWriteKey(entry.FullId)) && entry.StatusEnum != ContentStatus.Deleted)
			{
				TombstoneCachedContentObject(entry.FullId);
				return;
			}
			if (!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(entry.TypeName, out var type))
			{
				RemoveSchemaNotice(entry.FullId);
				_invalidContents.Remove(entry.FullId);
				_contentScriptableCache.Remove(entry.FullId);
				return;
			}
			
			if (entry.StatusEnum is ContentStatus.Deleted)
			{
				RemoveSchemaNotice(entry.FullId);
				ForgetDeletedItem(entry.FullId);
				TombstoneCachedContentObject(entry.FullId);
				
				var deletedObject = ScriptableObject.CreateInstance(type) as ContentObject;
				deletedObject.SetIdAndVersion(entry.FullId, String.Empty);
				deletedObject.Tags = entry.Tags.ToArray();
				deletedObject.SetContentName(entry.Name);
				deletedObject.ContentStatus = ContentStatus.Deleted;
				deletedObject.IsInConflict = entry.IsInConflict;
				_contentScriptableCache[entry.FullId] = deletedObject;
				Object.DontDestroyOnLoad(deletedObject);
				_invalidContents.Remove(entry.FullId);
				return;
			}
			
			
			if(!_contentScriptableCache.TryGetValue(entry.FullId, out var contentObject) || contentObject == null)
			{
				contentObject = ScriptableObject.CreateInstance(type) as ContentObject;
				_contentScriptableCache[entry.FullId] = contentObject;
			}
			else if (TryKeepEditsOverOwnSave(entry, contentObject))
			{
				ValidateForInvalidFields(contentObject);
				return;
			}

			contentObject = LoadContentObject(entry, contentObject);

			Object.DontDestroyOnLoad(contentObject);
			ValidateForInvalidFields(contentObject);
		}

		private ContentObject LoadContentObject(LocalContentManifestEntry entry, ContentObject contentObject)
		{
			var key = GetContentWriteKey(entry.FullId);
			var previousVersion = contentObject.Version;
			try
			{
				string fileContent;
				using (var stream = new FileStream(entry.JsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(stream))
					fileContent = reader.ReadToEnd();

				contentObject.CancelPendingEditorChangeNotification();
				contentObject.OnEditorChanged = null;
				var properties = ReadEditorProperties(contentObject, entry.FullId, fileContent, out var difference);
				UpdateSchemaDifference(entry, difference);
				ApplyEntryState(entry, contentObject);
				contentObject.ResetEditorChangeTracking();
				_lastSavedPropertiesCache.Remove(key);
				var identity = new ContentLoadIdentity(key);
				_loadedContent[key] = new LoadedContent(contentObject, entry.JsonFilePath, fileContent, properties, identity);
				_displayedLoads ??= new();
				_displayedLoads.Remove(contentObject);
				_displayedLoads.Add(contentObject, identity);
				contentObject.OnEditorChanged = () => SaveContent(contentObject);
				_loadErrors.Remove(key);
				return contentObject;
			}
			catch
			{
				contentObject.CancelPendingEditorChangeNotification();
				contentObject.OnEditorChanged = null;
				contentObject.SetIdAndVersion(entry.FullId, previousVersion);
				// The watcher must retry even if the repaired file equals the previous good baseline.
				_loadedContent.Remove(key);
				_loadErrors[key] = $"Could not load {entry.FullId}. Repair the local file and refresh to resume editing. See the Console for details.";
				ManifestChangedCount++;
				Debug.LogError($"Could not load content from json file. id=[{entry.FullId}] path=[{entry.JsonFilePath}]");
				throw;
			}
		}

		private static string ReadEditorProperties(ContentObject content, string id, string json, out SchemaDifference difference)
		{
			// Reset exactly the fields the Beamable deserializer can populate, including private
			// [ContentField] members. Keep their previous values until conversion and callbacks finish.
			var fields = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>().GetTypeFieldsCache().GetFieldInfos(content.GetType(), true);
			var previous = fields.Select(field => field.GetValue(content)).ToArray();
			var previousException = content.ContentException;
			var defaults = (ContentObject)ScriptableObject.CreateInstance(content.GetType());
			try
			{
				for (var i = 0; i < fields.Count; i++)
					fields[i].RawField.SetValue(content, fields[i].GetValue(defaults));
				content.ContentException = null;
				ClientContentSerializer.DeserializeContentFromCli(json, content, id, out difference, disableExceptions: true);
				return ClientContentSerializer.SerializeProperties(content);
			}
			catch
			{
				for (var i = 0; i < fields.Count; i++) fields[i].RawField.SetValue(content, previous[i]);
				content.ContentException = previousException;
				throw;
			}
			finally
			{
				defaults.CancelPendingEditorChangeNotification();
				Object.DestroyImmediate(defaults);
			}
		}

		private void RemoveContentFromCache(LocalContentManifestEntry entry)
		{
			if (TypeContentCache.TryGetValue(entry.TypeName, out var typeContentList))
			{
				typeContentList.RemoveAll(item => item.FullId == entry.FullId);
			}

			if (_statusContentCache.TryGetValue(entry.StatusEnum, out var statusContents))
			{
				statusContents.RemoveAll(item => item.FullId == entry.FullId);
			}
			
			_conflictedContentCache.Remove(entry.FullId);
			
			_invalidContents.Remove(entry.FullId);
			
			EntriesCache.Remove(entry.FullId);
			
			_contentIdTagsCache.Remove(entry.FullId);
		}

		public void ValidateForInvalidFields(ContentObject contentObject)
		{
			// No need to validate content that is deleted.
			if(contentObject.ContentStatus is ContentStatus.Deleted)
				return;
			bool hasValidationError =
				contentObject != null && contentObject.HasValidationErrors(ValidationContext, out var _);
			UpdateContentValidationStatus(contentObject.Id, hasValidationError);

		}

		private void HandleProgressUpdate(BeamContentProgressUpdateData data,
		                                string operationTitle,
		                                string onSuccessBaseMessage,
		                                string onErrorBaseMessage, ref int countItem)
		{
			int totalItems = Math.Max(data.totalItems, 0);
			string description = CurrentProgress.Description;
			float progress = totalItems == 0 ? 0 : Mathf.Clamp01((float)countItem / totalItems);
			int processedItemsForDisplay = countItem;

			switch (data.EventType)
			{
				case 0: // Error on Content Progress Update
					CurrentProgress.HasError = true;
					CurrentProgress.CurrentContentName = data.contentName;
					CurrentProgress.TotalItems = totalItems;
					CurrentProgress.Description = string.Format(onErrorBaseMessage, data.contentName, data.errorMessage);
					ProgressUpdateVersion++;
					if (_showUnityModalProgress)
					{
						EditorUtility.ClearProgressBar();
					}
					EditorUtility.DisplayDialog(
						operationTitle,
						string.Format(onErrorBaseMessage, data.contentName, data.errorMessage),
						"Okay"
					);
					return;
				case 1: // Sync Complete
				case 2: // Publish Complete
					CurrentProgress.CurrentContentName = data.contentName;
					int visibleCount = totalItems == 0 ? 0 : Mathf.Min(totalItems, countItem + 1);
					processedItemsForDisplay = visibleCount;
					description = string.Format(onSuccessBaseMessage, data.contentName, visibleCount, totalItems);
					progress = totalItems == 0 ? 1 : Mathf.Clamp01((float)visibleCount / totalItems);
					break;
				case 3: // Update Processed item count
					countItem = data.processedItems;
					processedItemsForDisplay = totalItems == 0 ? countItem : Mathf.Min(countItem, totalItems);
					progress = totalItems == 0 ? 1 : Mathf.Clamp01((float)processedItemsForDisplay / totalItems);
					description = totalItems == 0
						? $"{operationTitle} complete."
						: $"{processedItemsForDisplay}/{totalItems} items processed.";
					break;
			}

			CurrentProgress.Title = operationTitle;
			CurrentProgress.Description = description;
			CurrentProgress.ProcessedItems = processedItemsForDisplay;
			CurrentProgress.TotalItems = totalItems;
			CurrentProgress.Progress = progress;
			ProgressUpdateVersion++;

			if (_showUnityModalProgress)
			{
				EditorUtility.DisplayProgressBar(operationTitle, description, progress);
			}
		}

		public void SetManifestId(string id)
		{
			manifestIdOverride = id;
			var _ = Reload();
		}

		public bool TryGetContentObject(string entryId, out ContentObject content)
		{
			content = null;
			if (_contentScriptableCache.TryGetValue(entryId, out content) && content != null)
				return true;
			
			// if we don't have on cache or content is null because Unity decided to kill it
			// we try to load it
			if (!EntriesCache.TryGetValue(entryId, out var entryData) || !File.Exists(entryData.JsonFilePath) ||
			    !_contentTypeReflectionCache.TryGetType(entryData.TypeName, out var type))
			{
				return false;
			}

			
			content = ScriptableObject.CreateInstance(type) as ContentObject;
			content = LoadContentObject(entryData, content);
			_contentScriptableCache[entryId] = content;
			return true;
		}
	}
}
