using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Linq;

namespace Editor.CliContentManager
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private StorageHandle<CliContentService> _handle;
		private readonly BeamCommands _cli;
		private ContentPsWrapper _contentWatcher;
		private readonly BeamEditorContext _beamContext;

		public LocalContentManifest CachedManifest { get; private set; }

		public event Action OnManifestUpdated;

		public CliContentService(BeamCommands cli, BeamEditorContext beamContext)
		{
			_cli = cli;
			_beamContext = beamContext;
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
				var currentManifest =
					report.data.RelevantManifestsAgainstLatest.FirstOrDefault(item => item.OwnerCid == currentCid &&
						                                                          item.OwnerPid == currentPid);
				CachedManifest = currentManifest;
				OnManifestUpdated?.Invoke();
			});
			_contentWatcher.Command.Run();
		}
		
		public void ReceiveStorageHandle(StorageHandle<CliContentService> handle)
		{
			_handle = handle;
		}
	}
}
