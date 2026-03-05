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
	public class CliContentService : IStorageHandler<CliContentService>, ILoadWithContext
	{
		private const string SYNC_OPERATION_TITLE = "Sync Contents";
		private const string SYNC_OPERATION_SUCCESS_BASE_MESSAGE = "{0} sync complete. {1}/{2} items synced.";
		private const string SYNC_OPERATION_ERROR_BASE_MESSAGE = "Error when syncing content {0}. Error message: {1}";
		private const string PUBLISH_OPERATION_TITLE = "Publish Contents";
		private const string PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE = "{0} published. {1}/{2} items published.";
		private const string ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE = "Error when publishing content {0}. Discarding publishes changes. Error message: {1}";

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
		private readonly Dictionary<string, List<string>> _contentIdTagsCache = new();
		private List<string> _tagsCache = new();
		private int syncedContents;
		private int publishedContents;
		
		[NonSerialized]
		public bool isReloading;
		public List<string> availableManifestIds;

		public Dictionary<string, LocalContentManifestEntry> EntriesCache { get; }

		private readonly Dictionary<string, ContentObject> _contentScriptableCache;
		public BeamContentPsProgressMessage LatestProgressUpdate;
		private readonly ContentConfiguration _contentConfiguration;

		private ValidationContext ValidationContext => _provider.GetService<ValidationContext>();
		public int ManifestChangedCount { get; private set; }

		public bool HasConflictedContent => _conflictedContentCache.Count > 0;

		public bool HasInvalidContent => _invalidContents.Count > 0;

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
			_ = Reload();
		}

		
		public void SaveContent(ContentObject selectedContentObject)
		{
			string propertiesJson = ClientContentSerializer.SerializeProperties(selectedContentObject);
			bool hasValidationError = selectedContentObject.HasValidationErrors(GetValidationContext(), out List<string> _);
			UpdateContentValidationStatus(selectedContentObject.Id, hasValidationError);
			SaveContent(selectedContentObject.Id, propertiesJson, () => SetContentTags(selectedContentObject.Id, selectedContentObject.Tags));
		}

		public void SaveContent(string contentId, string contentPropertiesJson, Action onCompleted = null)
		{
			var saveCommand = _cli.ContentSave(new ContentSaveArgs()
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				contentIds = new[] {contentId},
				contentProperties = new[] {contentPropertiesJson}
			});
			saveCommand.Run().Then(_ => { onCompleted?.Invoke(); });
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
			if (_contentWatcher != null)
			{
				_contentWatcher.Cancel();
				_contentWatcher = null;
			}

			var restoreWrapper = _cli.ContentRestore(new ContentRestoreArgs() {manifestId = GetSelectedManifestIdCliOption(), name = snapshotPath, deleteAfterRestore = deleteAfterRestore, additiveRestore = isAdditiveRestore});
			
			await restoreWrapper.Run();

			// Re-enable watcher
			await Reload();
		}

		public async Promise TakeSnapshot(string snapshotName, bool isLocal)
		{
			// Disable watcher when creating a snapshot to prevent changes on files during snapshot
			if (_contentWatcher != null)
			{
				_contentWatcher.Cancel();
				_contentWatcher = null;
			}

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
				if (_contentWatcher != null)
				{
					_contentWatcher.Cancel();
					_contentWatcher = null;
				}

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
		                                           ContentFilterType filterType = default)
		{

			syncedContents = 0;
			EditorUtility.DisplayProgressBar(SYNC_OPERATION_TITLE, "Synchronizing contents...", 0);
			try
			{
				if (_contentWatcher != null)
				{
					_contentWatcher.Cancel();
					_contentWatcher = null;
				}
				var contentSyncCommand = _cli.ContentSync(new ContentSyncArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					syncModified = syncModified,
					syncConflicts = syncConflicted,
					syncCreated = syncCreated,
					syncDeleted = syncDeleted,
					filter = filter,
					filterType = filterType,
					
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
			});
			
			await contentSyncCommand.Run();
		}

		public void SetContentTags(string contentId, string[] tags)
		{
			if (EntriesCache.TryGetValue(contentId, out var entry))
			{
				entry.Tags = tags;
				EntriesCache[contentId] = entry;
			}

			if (tags.Length == 0)
			{
				var setContentTagCommand = _cli.ContentTagSet(new ContentTagSetArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					filterType = ContentFilterType.ExactIds,
					filter = contentId,
					clear = true,
					tag = "<none>"
				});
				setContentTagCommand.Run();
			}
			else
			{
				var setContentTagCommand = _cli.ContentTagSet(new ContentTagSetArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					filterType = ContentFilterType.ExactIds,
					filter = contentId,
					tag = string.Join(",", tags)
				});
				setContentTagCommand.Run();
			}

		}

		public bool DuplicateContent(LocalContentManifestEntry entry, out ContentObject duplicatedObject)
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
				entry.Name = duplicatedObject.ContentName;
				entry.FullId = duplicatedObject.Id;
				entry.TypeName = duplicatedObject.ContentType;
				AddContentToCache(entry);
				SaveContent(duplicatedObject);
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
				RemoveContentFromCache(entry);
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				AddContentToCache(entry);
				entry.Name = newName;
				entry.CurrentStatus = (int)ContentStatus.Created;
				entry.FullId = newFullId;
				AddContentToCache(entry);
			}
			else
			{
				RemoveContentFromCache(entry);
				entry.Name = newName;
				entry.FullId = newFullId;
				AddContentToCache(entry);
			}

			ManifestChangedCount++;

			BeamUnityFileUtils.RenameFile(originalPath, fullName);
			
			return newFullId;
		}

		private bool ValidateNewContentName(string newName)
		{
			return Path.GetInvalidFileNameChars().All(invalidFileNameChar => !newName.Contains(invalidFileNameChar));
		}

		public void DeleteContent(string contentId)
		{
			if (!EntriesCache.TryGetValue(contentId, out var entry))
			{
				Debug.LogError($"No Content found with id: {contentId}");
				return;
			}
			
			if (entry.StatusEnum is not ContentStatus.Created)
			{
				RemoveContentFromCache(entry);
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				AddContentToCache(entry);
			}

			File.Delete(entry.JsonFilePath);
		}

		public void ResolveConflict(string contentId, bool useLocal)
		{
			var resolveCommand = _cli.ContentResolve(new ContentResolveArgs()
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				filter = contentId,
				filterType = ContentFilterType.ExactIds,
				use = useLocal ? "local" : "realm"
			});
			resolveCommand.Run();
			var content = EntriesCache[contentId];
			content.IsInConflict = false;
			EntriesCache[contentId] = content;
		}
		
		public async Promise PublishContentsWithProgress()
		{
			EditorUtility.DisplayProgressBar(PUBLISH_OPERATION_TITLE, "Publishing contents...", 0);
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
				publishCommand.OnProgressStreamContentProgressUpdateData(report =>
				{
					HandleProgressUpdate(report.data, PUBLISH_OPERATION_TITLE, PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE,
					                     ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE, ref publishedContents);
				});
				await publishCommand.Run();
			}
			finally
			{
				FinishActionProgress();
			}
		}

		private void FinishActionProgress()
		{
			EditorUtility.ClearProgressBar();
			_ = Reload();
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

		public async Promise Reload()
		{
			isReloading = true;
			try
			{
				ClearCaches();
				if (_contentWatcher != null)
				{
					_contentWatcher.Cancel();
					_contentWatcher = null;
				}

				TaskCompletionSource<bool> manifestIsFetchedTaskCompletion = new TaskCompletionSource<bool>();


				_contentWatcher = _cli.ContentPs(new ContentPsArgs()
				{
					manifestIds = GetSelectedManifestIdsCliOption(),
					watch = true
				});

				void OnDataReceived(ReportDataPoint<BeamContentPsCommandEvent> report)
				{
					string currentCid = _beamContext.BeamCli.CurrentRealm.Cid;
					string currentPid = _beamContext.BeamCli.CurrentRealm.Pid;

					bool ValidateManifest(LocalContentManifest item) =>
						item.OwnerCid == currentCid && item.OwnerPid == currentPid;

					var reportData = report.data;
					var changesManifest = reportData.RelevantManifestsAgainstLatest.FirstOrDefault(ValidateManifest);
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

							AddContentToCache(entry);
						}
					}

					if (hasRemoveManifest)
					{
						foreach (var entry in removeManifest.Entries)
						{
							if (EntriesCache.TryGetValue(entry.FullId, out LocalContentManifestEntry oldEntry))
							{
								RemoveContentFromCache(oldEntry);
								if (oldEntry.StatusEnum is not ContentStatus.Created)
								{
									AddContentToCache(entry);
								}
							}

							ValidationContext.AllContent.Remove(entry.FullId);
						}
					}

					if (hasRemoveManifest || hasChangeManifest)
					{
						ManifestChangedCount++;
					}

					if (!manifestIsFetchedTaskCompletion.Task.IsCompleted)
					{
						manifestIsFetchedTaskCompletion.SetResult(true);
					}

					ValidationContext.Initialized = true;
					ComputeTags();
					ValidateAllContents();
				}

				_contentWatcher.OnStreamContentPsCommandEvent(OnDataReceived);
				_contentWatcher.OnProgressStreamContentPsProgressMessage(dp => { LatestProgressUpdate = dp.data; });
				_ = _contentWatcher.Command.Run();

				bool addedAnyDefault = false;
				var getAvailableManifestsPromise = _cli.ContentListManifests(new ContentListManifestsArgs()).OnStreamContentListManifestsCommandResults(dp =>
				{
					
					var manifestIds = new HashSet<string>();
					foreach (var id in dp.data.localManifests)
					{
						manifestIds.Add(id);
					}

					foreach (var id in dp.data.remoteManifests)
					{
						manifestIds.Add(id);
					}
					
					if (dp.data.remoteManifests.Count == 0)
					{
						// If no remote manifest on remote, it means that it is the first time that the customer is using this realm
						// If so, we need to create the default contents
						string[] guids = BeamableAssetDatabase.FindAssets<ContentObject>(new[] {Constants.Directories.DEFAULT_DATA_DIR});
						foreach (string guid in guids)
						{
							string path = AssetDatabase.GUIDToAssetPath(guid);
							ContentObject obj = AssetDatabase.LoadAssetAtPath<ContentObject>(path);

							if (obj == null)
								continue;

							string fileName = Path.GetFileNameWithoutExtension(path);
							obj.SetContentName(fileName);
							SaveContent(obj);
							addedAnyDefault = true;
						}
					}

					availableManifestIds = manifestIds.ToList();
				}).OnError(dp =>
				{
					Debug.LogError(dp.data.message);
				}).Run();
				
				await manifestIsFetchedTaskCompletion.Task;
				await getAvailableManifestsPromise;

				if (addedAnyDefault)
				{
					await PublishContents();
				}

				ManifestChangedCount++;
			}
			finally
			{
				isReloading = false;
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

		private void AddContentToCache(LocalContentManifestEntry entry)
		{
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
			
			CacheScriptableContent(entry);

			if (entry.StatusEnum is not ContentStatus.Deleted)
			{
				ValidationContext.AllContent[entry.FullId] = entry;
			}
		}

		private void CacheScriptableContent(LocalContentManifestEntry entry)
		{
			if (!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(entry.TypeName, out var type))
			{
				_invalidContents.Remove(entry.FullId);
				_contentScriptableCache.Remove(entry.FullId);
				return;
			}
			
			if (entry.StatusEnum is ContentStatus.Deleted)
			{
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
			
			contentObject = LoadContentObject(entry, contentObject);

			Object.DontDestroyOnLoad(contentObject);
			ValidateForInvalidFields(contentObject);
		}

		private ContentObject LoadContentObject(LocalContentManifestEntry entry, ContentObject contentObject)
		{
			string fileContent;
			using (FileStream fileStream = new FileStream(entry.JsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader streamReader = new StreamReader(fileStream))
			{
				fileContent = streamReader.ReadToEnd();
			}
			try
			{
				contentObject =
					ClientContentSerializer.DeserializeContentFromCli(fileContent, contentObject, entry.FullId, out SchemaDifference schemaDifference,
					                                                  disableExceptions: true) as ContentObject;
				if (contentObject)
				{
					contentObject.Tags = entry.Tags.ToArray();
					contentObject.ContentStatus = entry.StatusEnum;
					contentObject.IsInConflict = entry.IsInConflict;
					contentObject.OnEditorChanged = () => { SaveContent(contentObject); };
					if (_contentConfiguration.validateSchemaDifference && schemaDifference != SchemaDifference.None)
					{
						Debug.LogWarning($"Schema for content with id=[{entry.FullId}] is different from content JSON. reason=[{schemaDifference}]\nPlease update the scriptable object to Match the JSON or Publish the local content.\nJson: {fileContent}");
						SaveContent(contentObject);
					}
				}
			}
			catch
			{
				Debug.LogError($"Could not load content from json file. id=[{entry.FullId}] path=[{entry.JsonFilePath}]");
				throw;
			}

			return contentObject;
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
			string description = string.Empty;
			float progress = (float)countItem / data.totalItems;

			switch (data.EventType)
			{
				case 0: // Error on Content Progress Update
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog(
						operationTitle,
						string.Format(onErrorBaseMessage, data.contentName, data.errorMessage),
						"Okay"
					);
					return;
				case 1: // Sync Complete
				case 2: // Publish Complete
					description = string.Format(onSuccessBaseMessage, data.contentName, countItem, data.totalItems);
					break;
				case 3: // Update Processed item count
					countItem = data.processedItems;
					break;
			}

			EditorUtility.DisplayProgressBar(operationTitle, description, progress);
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
