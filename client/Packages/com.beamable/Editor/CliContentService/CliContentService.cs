using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
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
				contentIds = new[] {contentId}, contentProperties = new[] {contentPropertiesJson},
			});
			saveCommand.Run();
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
