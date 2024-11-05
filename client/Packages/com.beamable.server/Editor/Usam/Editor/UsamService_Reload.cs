using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;

namespace Beamable.Server.Editor.Usam
{
	public partial class UsamService
	{


		public void Reload()
		{
			var _ = WaitReload();
		}

		public Promise WaitReload()
		{
			var taskId = ++latestReloadTaskId;

			LoadLegacyServices();

			var command = _cli.UnityManifest();
			command.OnStreamShowManifestCommandOutput(cb =>
			{
				if (latestReloadTaskId != taskId)
					return;

				hasReceivedManifestThisDomain = true;
				latestManifest = cb.data;

				foreach (var service in latestManifest.services)
				{
					service.Flags = BeamManifestEntryFlags.IS_SERVICE;
					var isReadonly = service.IsReadonlyPackage = BeamManifestServiceEntry.IsReadonlyProject(service.csprojPath);
					if (isReadonly)
					{
						service.Flags |= BeamManifestEntryFlags.IS_READONLY;
					}
				}

				foreach (var storage in latestManifest.storages)
				{
					storage.Flags = BeamManifestEntryFlags.IS_STORAGE;
					var isReadonly = storage.IsReadonlyPackage = BeamManifestServiceEntry.IsReadonlyProject(storage.csprojPath);
					if (isReadonly)
					{
						storage.Flags |= BeamManifestEntryFlags.IS_READONLY;
					}

				}

				CsProjUtil.OnPreGeneratingCSProjectFiles(this);
			});

			var p = command.Run();
			ListenForStatus();
			ListenForDocker();
			ListenForBuildChanges();
			return p;
		}

	}
}
