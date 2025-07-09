using Beamable;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.ContentService
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private const string SYNC_OPERATION_TITLE = "Sync Contents";
		private const string SYNC_OPERATION_SUCCESS_BASE_MESSAGE = "{0} sync complete";
		private const string SYNC_OPERATION_ERROR_BASE_MESSAGE = "Error when syncing content {0}. Error message: {1}";
		private const string PUBLISH_OPERATION_TITLE = "Publish Contents";
		private const string PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE = "{0} published";
		private const string ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE = "Error when publishing content {0}. Discarding publishes changes. Error message: {1}";

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

		public Dictionary<string, LocalContentManifestEntry> CachedManifest { get; }

		private ValidationContext ValidationContext => _provider.GetService<ValidationContext>();

		public event Action OnManifestUpdated;
		public event Action OnServiceReload;

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

		public CliContentService(BeamCommands cli, BeamEditorContext beamContext, IDependencyProvider provider)
		{
			_cli = cli;
			_beamContext = beamContext;
			_provider = provider;
			CachedManifest = new();
			ContentObject.ValidationContext = ValidationContext;
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
		}


		public void SaveContent(string contentId, string contentPropertiesJson)
		{
			var saveCommand = _cli.ContentSave(new ContentSaveArgs()
			{
				contentIds = new[] {contentId},
				contentProperties = new[] {contentPropertiesJson}
			});
			saveCommand.Run();
		}

		public async Task SyncContentsWithProgress(bool syncModified,
		                                           bool syncCreated,
		                                           bool syncConflicted,
		                                           bool syncDeleted,
		                                           string filter = null,
		                                           ContentFilterType filterType = default)
		{

			syncedContents = 0;
			
			try
			{
				var contentSyncCommand = _cli.ContentSync(new ContentSyncArgs()
				{
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
					                     SYNC_OPERATION_ERROR_BASE_MESSAGE, ref syncedContents, () =>
					                     {
						                     contentSyncCommand.Cancel();
						                     EditorUtility.ClearProgressBar();
					                     });
				});
				await contentSyncCommand.Run();

				if (EditorUtility.DisplayCancelableProgressBar(SYNC_OPERATION_TITLE, "Synchronizing contents...", 0))
				{
					contentSyncCommand.Cancel();
					EditorUtility.ClearProgressBar();
				}

			}
			finally
			{
				EditorUtility.ClearProgressBar();
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
			if (!CachedManifest.TryGetValue(contentId, out var entry))
			{
				Debug.Log($"No Entry found with id: {contentId}");
				return;
			}

			entry.Tags = tags;
			CachedManifest[contentId] = entry;

			var setContentTagCommand = _cli.ContentTagSet(new ContentTagSetArgs()
			{
				filterType = ContentFilterType.ExactIds,
				filter = contentId,
				tag = string.Join(",", tags)
			});
			setContentTagCommand.Run();
		}

		public void RenameContent(string contentId, string newName)
		{
			if (!CachedManifest.TryGetValue(contentId, out var entry))
			{
				Debug.Log($"No Entry found with id: {contentId}");
				return;
			}

			if (!ValidateNewContentName(newName))
			{
				Debug.Log(
					$"{newName} contains invalid characters ({string.Join(", ", Path.GetInvalidFileNameChars())}");
				return;
			}

			var fullName = $"{entry.TypeName}.{newName}.json";

			string fileDirectoryPath = Path.GetDirectoryName(entry.JsonFilePath);
			string newFileNamePath = Path.Combine(fileDirectoryPath, fullName);
			if (File.Exists(newFileNamePath))
			{
				Debug.LogWarning($"A Content already exists with name: {newName}");
				return;
			}

			// Apply local changes while CLI is updating the Data so Unity UI doesn't take long to update.
			if (entry.StatusEnum is not ContentStatus.Created)
			{
				RemoveContentFromCache(entry);
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				AddContentToCache(entry);
				entry.Name = newName;
				entry.CurrentStatus = (int)ContentStatus.Created;
				entry.FullId = $"{entry.TypeName}.{entry.Name}";
				AddContentToCache(entry);
			}

			BeamUnityFileUtils.RenameFile(entry.JsonFilePath, fullName);
		}

		private bool ValidateNewContentName(string newName)
		{
			return Path.GetInvalidFileNameChars().All(invalidFileNameChar => !newName.Contains(invalidFileNameChar));
		}

		public void DeleteContent(string contentId)
		{
			if (!CachedManifest.TryGetValue(contentId, out var entry))
			{
				Debug.Log($"No Content found with id: {contentId}");
				return;
			}

			RemoveContentFromCache(entry);
			if (entry.StatusEnum is not ContentStatus.Created)
			{
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				AddContentToCache(entry);
			}

			File.Delete(entry.JsonFilePath);
		}

		public void ResolveConflict(string contentId, bool useLocal)
		{
			var resolveCommand = _cli.ContentResolve(new ContentResolveArgs()
			{
				filter = contentId,
				filterType = ContentFilterType.ExactIds,
				use = useLocal ? "local" : "realm"
			});
			resolveCommand.Run();
			var content = CachedManifest[contentId];
			content.IsInConflict = false;
			CachedManifest[contentId] = content;
		}
		
		public async Task PublishContentsWithProgress()
		{
			publishedContents = 0;

			try
			{
				var publishCommand = _cli.ContentPublish(new ContentPublishArgs());
				publishCommand.OnProgressStreamContentProgressUpdateData(report =>
				{
					HandleProgressUpdate(report.data, PUBLISH_OPERATION_TITLE, PUBLISH_OPERATION_SUCCESS_BASE_MESSAGE,
					                     ERROR_PUBLISH_OPERATION_ERROR_BASE_MESSAGE, ref publishedContents, () =>
					                     {
						                     publishCommand.Cancel();
						                     EditorUtility.ClearProgressBar();
					                     });
				});
				await publishCommand.Run();

				if (EditorUtility.DisplayCancelableProgressBar(PUBLISH_OPERATION_TITLE, "Publishing contents...", 0))
				{
					publishCommand.Cancel();
					EditorUtility.ClearProgressBar();
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		public async Task PublishContents()
		{
			var publishCommand = _cli.ContentPublish(new ContentPublishArgs());
			await publishCommand.Run();
		}

		public async Promise Reload()
		{
			ClearCaches();
			if (_contentWatcher != null)
			{
				_contentWatcher.Cancel();
				_contentWatcher = null;
			}

			TaskCompletionSource<bool> manifestIsFetchedTaskCompletion = new TaskCompletionSource<bool>();

			_contentWatcher = _cli.ContentPs(new ContentPsArgs() {watch = true});

			void OnDataReceived(ReportDataPoint<BeamContentPsCommandEvent> report)
			{
				string currentCid = _beamContext.CurrentRealm.Cid;
				string currentPid = _beamContext.CurrentRealm.Pid;

				bool ValidateManifest(LocalContentManifest item) => item.OwnerCid == currentCid && item.OwnerPid == currentPid;

				var reportData = report.data;
				var changesManifest = reportData.RelevantManifestsAgainstLatest.FirstOrDefault(ValidateManifest);
				var removeManifest = reportData.ToRemoveLocalEntries.FirstOrDefault(ValidateManifest);

				bool hasChangeManifest = changesManifest != null && changesManifest.Entries.Length > 0;
				bool hasRemoveManifest = removeManifest != null && removeManifest.Entries.Length > 0;

				if (hasChangeManifest)
				{
					foreach (var entry in changesManifest.Entries)
					{
						_ = ValidateForInvalidFields(entry);

						if (CachedManifest.TryGetValue(entry.FullId, out LocalContentManifestEntry oldEntry))
						{
							RemoveContentFromCache(oldEntry);
						}
						
						ValidationContext.AllContent[entry.FullId] = entry;
						AddContentToCache(entry);
					}
				}

				if (hasRemoveManifest)
				{
					foreach (var entry in removeManifest.Entries)
					{
						if (CachedManifest.TryGetValue(entry.FullId, out LocalContentManifestEntry oldEntry))
						{
							RemoveContentFromCache(oldEntry);
						}

						if (oldEntry.StatusEnum is not ContentStatus.Created)
						{
							AddContentToCache(entry);
						}

						ValidationContext.AllContent.Remove(entry.FullId);
					}
				}

				if (hasRemoveManifest || hasChangeManifest)
				{
					OnManifestUpdated?.Invoke();
				}

				if (!manifestIsFetchedTaskCompletion.Task.IsCompleted)
				{
					manifestIsFetchedTaskCompletion.SetResult(true);
				}

				ValidationContext.Initialized = true;
				ComputeTags();
			}

			_contentWatcher.OnStreamContentPsCommandEvent(OnDataReceived);
			_ = _contentWatcher.Command.Run();

			await manifestIsFetchedTaskCompletion.Task;
			OnServiceReload?.Invoke();
		}

		private void ClearCaches()
		{
			ValidationContext.AllContent.Clear();
			CachedManifest.Clear();
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

			return api.CliContentService.CachedManifest.Any(item => item.Value.StatusEnum is not ContentStatus
				                                                .UpToDate);
		}

		public IValidationContext GetValidationContext()
		{
			return ValidationContext;
		}

		public List<LocalContentManifestEntry> GetContentsFromType(Type contentType)
		{
			var contentTypeName = ContentTypeReflectionCache.GetContentTypeName(contentType);
			return GetContentFromTypeName(contentTypeName);
		}

		public List<LocalContentManifestEntry> GetContentFromTypeName(string contentTypeName)
		{
			if (!TypeContentCache.TryGetValue(contentTypeName, out var typeContents))
			{
				return new List<LocalContentManifestEntry>();
			}

			return typeContents;
		}

		public List<LocalContentManifestEntry> GetAllContentFromStatus(ContentStatus status)
		{
			if (!_statusContentCache.TryGetValue(status, out var contents))
			{
				return new  List<LocalContentManifestEntry>();
			}
			return contents;
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
			
			if (hasValidationError && CachedManifest.TryGetValue(itemId, out var entry))
			{
				_invalidContents[entry.FullId] = entry;
			}
			else
			{
				_invalidContents.Remove(itemId);
			}
		}

		private void AddContentToCache(LocalContentManifestEntry entry)
		{
			if (!TypeContentCache.TryGetValue(entry.TypeName, out var typeContentList))
			{
				TypeContentCache[entry.TypeName] = typeContentList = new List<LocalContentManifestEntry>();;
			}
			
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
			
			CachedManifest[entry.FullId] = entry;
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
			
			CachedManifest.Remove(entry.FullId);
			
			_contentIdTagsCache.Remove(entry.FullId);
		}

		private async Task ValidateForInvalidFields(LocalContentManifestEntry entry)
		{
			if (!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(entry.TypeName, out var type) ||
			    !File.Exists(entry.JsonFilePath))
			{
				_invalidContents.Remove(entry.FullId);
				return;
			}

			string fileContent = await LoadContentFileData(entry);
			var contentObject = ScriptableObject.CreateInstance(type) as ContentObject;
			contentObject =
				ClientContentSerializer.DeserializeContentFromCli(fileContent, contentObject, entry.FullId) as
					ContentObject;
			bool hasValidationError =
				contentObject != null && contentObject.HasValidationErrors(ValidationContext, out var _);
			UpdateContentValidationStatus(entry.FullId, hasValidationError);

		}

		private void HandleProgressUpdate(BeamContentProgressUpdateData data,
		                                string operationTitle,
		                                string onSuccessBaseMessage,
		                                string onErrorBaseMessage, ref int countItem, Action onCancelOperation)
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
					description = string.Format(onSuccessBaseMessage, data.contentName);
					break;
				case 3: // Update Processed item count
					countItem = data.processedItems;
					break;
			}

			if (EditorUtility.DisplayCancelableProgressBar(operationTitle, description, progress))
			{
				onCancelOperation?.Invoke();
			}
		}
	}
}
