using Beamable;
using Beamable.Common;
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
using Object = UnityEngine.Object;

namespace Editor.ContentService
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private const string SYNC_OPERATION_TITLE = "Sync Contents";
		
		private StorageHandle<CliContentService> _handle;
		private readonly BeamCommands _cli;
		private ContentPsWrapper _contentWatcher;
		private readonly BeamEditorContext _beamContext;
		private readonly IDependencyProvider _provider;
		private Dictionary<string, List<LocalContentManifestEntry>> _typeContentCache =  new();
		private readonly ContentTypeReflectionCache _contentTypeReflectionCache;
		private HashSet<string> _invalidContents = new();
		private int syncedContents;

		public Dictionary<string, LocalContentManifestEntry> CachedManifest { get; }
		
		private ValidationContext ValidationContext => _provider.GetService<ValidationContext>();

		public event Action OnManifestUpdated;

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
				contentIds = new[] {contentId}, contentProperties = new[] {contentPropertiesJson}
			});
			saveCommand.Run();
		}

		public async Task SyncContentsWithProgress(bool syncModified,
		                                           bool syncNew,
		                                           bool syncConflicted,
		                                           bool syncDeleted,
		                                           string filter = null,
		                                           ContentFilterType filterType = default)
		{

			syncedContents = 0;
			EditorUtility.DisplayProgressBar(SYNC_OPERATION_TITLE, "Synchronizing contents...", 0);

			try
			{
				await SyncContents(
					syncModified,
					syncNew,
					syncConflicted,
					syncDeleted,
					filter,
					filterType,
					onProgressUpdate: HandleSyncProgress
				);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}
		
		private void HandleSyncProgress(BeamContentSyncProgressUpdateData data)
		{
			string description = string.Empty;
			float progress = (float)syncedContents / data.itemsToRevert;
			Debug.Log($"Progress:{progress}");

			switch (data.EventType)
			{
				case 0: // Sync Complete
					description = $"Content {data.contentName} synced...";
					break;
				case 1: // Error on Sync Content
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog(
						SYNC_OPERATION_TITLE,
						$"Error when syncing content {data.contentName}. Error message: {data.errorMessage}",
						"Okay"
					);
					return;
			}

			EditorUtility.DisplayProgressBar(SYNC_OPERATION_TITLE, description, progress);
		}
		
		public async Task SyncContents(bool syncModified = true,
		                         bool syncCreated = true,
		                         bool syncConflicted = true,
		                         bool syncDeleted = true,
		                         string filter = null,
		                         ContentFilterType filterType = default, Action<BeamContentSyncProgressUpdateData> onProgressUpdate = null)
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

			contentSyncCommand.OnProgressStreamContentSyncProgressUpdateData(reportData =>
			{
				onProgressUpdate?.Invoke(reportData.data);
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
				filterType = ContentFilterType.ExactIds, filter = contentId, tag = string.Join(",", tags)
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
				Debug.Log($"{newName} contains invalid characters ({string.Join(", ", Path.GetInvalidFileNameChars())}");
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
			if (entry.StatusEnum is ContentStatus.Created)
			{
				entry.Name = newName;
				CachedManifest[contentId] = entry;
			}
			else
			{
				entry.CurrentStatus = (int)ContentStatus.Deleted;
				CachedManifest[contentId] = entry;
				entry.Name = newName;
				entry.CurrentStatus = (int)ContentStatus.Created;
				var newId = $"{entry.TypeName}.{entry.Name}";
				CachedManifest[newId] = entry;
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

			entry.CurrentStatus = (int)ContentStatus.Deleted;
			CachedManifest[contentId] = entry;
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

		public async Task Publish()
		{
			var publishCommand = _cli.ContentPublish(new ContentPublishArgs());
			await publishCommand.Run();
		}

		public async Promise Reload()
		{
			ValidationContext.AllContent.Clear();
			CachedManifest.Clear();
			_typeContentCache.Clear();
			_invalidContents.Clear();
			if (_contentWatcher != null)
			{
				_contentWatcher.Cancel();
				_contentWatcher = null;
			}
			
			TaskCompletionSource<bool> manifestIsFetchedTaskCompletion = new TaskCompletionSource<bool>();
			
			_contentWatcher = _cli.ContentPs(new ContentPsArgs() {watch = true});
			_contentWatcher.OnStreamContentPsCommandEvent(async report =>
			{
				string currentCid = _beamContext.CurrentRealm.Cid;
				string currentPid = _beamContext.CurrentRealm.Pid;
				
				bool ValidateManifest(LocalContentManifest item) => item.OwnerCid == currentCid && item.OwnerPid == currentPid;
				
				var reportData = report.data;
				var changesManifest = reportData.RelevantManifestsAgainstLatest.FirstOrDefault(ValidateManifest);
				var removeManifest = reportData.ToRemoveLocalEntries.FirstOrDefault(ValidateManifest);
				
				bool hasChangeManifest = changesManifest != null;
				bool hasRemoveManifest = removeManifest != null;
				
				if (hasChangeManifest)
				{
					foreach (var entry in changesManifest.Entries)
					{

						await ValidateForInvalidFields(entry);
						
						CachedManifest[entry.FullId] =  entry;
						ValidationContext.AllContent[entry.FullId] = entry;
						AddContentToCache(entry);
					}
				}
				
				
				if (hasRemoveManifest)
				{
					foreach (var entry in removeManifest.Entries)
					{
						_invalidContents.Remove(entry.FullId);
						CachedManifest.Remove(entry.FullId);
						ValidationContext.AllContent.Remove(entry.FullId);
						RemoveContentFromCache(entry);
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

			});
			_ = _contentWatcher.Command.Run();
			
			await manifestIsFetchedTaskCompletion.Task;
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

			return api.CliContentService.CachedManifest.Any(item => item.Value.StatusEnum is not ContentStatus.UpToDate);
		}

		public IValidationContext GetValidationContext()
		{
			return ValidationContext;
		}

		public List<LocalContentManifestEntry> GetContentsFromType(Type contentType)
		{
			var contentTypeName = ContentTypeReflectionCache.GetContentTypeName(contentType);
			if (!_typeContentCache.TryGetValue(contentTypeName, out var typeContents))
			{
				return new List<LocalContentManifestEntry>();
			}

			return typeContents;
		}
		
		public static async Task<string> LoadContentFileData(LocalContentManifestEntry entry)
		{
			FileStream fileStream = new FileStream(entry.JsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			StreamReader streamReader = new StreamReader(fileStream);

			string fileContent = await streamReader.ReadToEndAsync();
			streamReader.Close();
			fileStream.Close();
			return fileContent;
		}

		public bool IsContentInvalid(string entryId)
		{
			return _invalidContents.Contains(entryId);
		}
		
		public void UpdateContentValidationStatus(string fullId, bool hasValidationError)
		{
			if (hasValidationError)
			{
				_invalidContents.Add(fullId);	
			}
			else
			{
				_invalidContents.Remove(fullId);
			}
		}

		private void AddContentToCache(LocalContentManifestEntry entry)
		{
			if (!_typeContentCache.TryGetValue(entry.TypeName, out var typeContentList))
			{
				typeContentList = new List<LocalContentManifestEntry>();
				_typeContentCache.Add(entry.TypeName, typeContentList);
			}
			typeContentList.Add(entry);
		}

		private void RemoveContentFromCache(LocalContentManifestEntry entry)
		{
			if (_typeContentCache.TryGetValue(entry.TypeName, out var typeContentList))
			{
				typeContentList.RemoveAll(item => item.FullId == entry.FullId);
			}
		}
		
		private async Task ValidateForInvalidFields(LocalContentManifestEntry entry)
		{
			if(!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(entry.TypeName, out var type) || !File.Exists(entry.JsonFilePath))
			{
				_invalidContents.Remove(entry.FullId);
				return;
			}

			string fileContent = await LoadContentFileData(entry);
			var contentObject = ScriptableObject.CreateInstance(type) as ContentObject;
			contentObject = ClientContentSerializer.DeserializeContentFromCli(fileContent, contentObject, entry.FullId) as ContentObject;
			bool hasValidationError = contentObject != null && contentObject.HasValidationErrors(ValidationContext, out var _);
			UpdateContentValidationStatus(entry.FullId, hasValidationError);
			
		}
	}
}
