using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Editor.CliContentManager
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private StorageHandle<CliContentService> _handle;
		private readonly BeamCommands _cli;
		private ContentPsWrapper _contentWatcher;
		private readonly BeamEditorContext _beamContext;

		public Dictionary<string, LocalContentManifestEntry> CachedManifest { get; }

		public event Action OnManifestUpdated;

		public CliContentService(BeamCommands cli, BeamEditorContext beamContext)
		{
			_cli = cli;
			_beamContext = beamContext;
			CachedManifest = new();
		}

		
		public void SaveContent(string contentId, string contentPropertiesJson)
		{
			var saveCommand = _cli.ContentSave(new ContentSaveArgs()
			{
				contentIds = new[] {contentId}, contentProperties = new[] {contentPropertiesJson}
			});
			saveCommand.Run();
		}

		public void SyncContents(bool syncModified = true, 
		                         bool syncCreated = true, 
		                         bool syncConflicted = true, 
		                         string filter = null, 
		                         ContentFilterType filterType = default)
		{
			var contentSyncCommand = _cli.ContentSync(new ContentSyncArgs()
			{
				syncModified = syncModified,
				syncConflicts = syncConflicted,
				syncCreated = syncCreated,
				filter = filter,
				filterType = filterType,
			});
			contentSyncCommand.Run();
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
			
			string fileDirectoryPath = Path.GetDirectoryName(entry.JsonFilePath);
			string newFileNamePath = Path.Combine(fileDirectoryPath, newName);
			if (File.Exists(newFileNamePath))
			{
				Debug.Log($"A Content already exists with name: {newName}");
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
			
			BeamUnityFileUtils.RenameFile(entry.JsonFilePath, $"{entry.TypeName}.{newName}.json");
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
		}

		public void Publish()
		{
			var publishCommand = _cli.ContentPublish(new ContentPublishArgs());
			publishCommand.Run();
		}

		public void Reload()
		{
			CachedManifest.Clear();
			if (_contentWatcher != null)
			{
				_contentWatcher.Cancel();
				_contentWatcher = null;
			}
			_contentWatcher = _cli.ContentPs(new ContentPsArgs() {watch = true});
			_contentWatcher.OnStreamContentPsCommandEvent(report =>
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
						CachedManifest[entry.FullId] =  entry;
					}
				}
				
				
				if (hasRemoveManifest)
				{
					foreach (var entry in removeManifest.Entries)
					{
						CachedManifest.Remove(entry.FullId);
					}
				}

				if (hasRemoveManifest || hasChangeManifest)
				{
					OnManifestUpdated?.Invoke();
				}

			});
			_contentWatcher.Command.Run();
		}
		
		public void ReceiveStorageHandle(StorageHandle<CliContentService> handle)
		{
			_handle = handle;
		}
	}
}
