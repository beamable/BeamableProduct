using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.Extensions;
using System.Linq;
using UnityEditor;

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
			LoadAllAssemblies();
			
			_ctx.OnRealmChange -= OnBeamEditorRealmChanged;
			_ctx.OnRealmChange += OnBeamEditorRealmChanged;

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

		private void OnBeamEditorRealmChanged(RealmView realm)
		{
			if (latestStatus?.Any(x => x.availableRoutes?.HasAnyLocalInstances() ?? false) ?? false)
			{EditorUtility.DisplayDialog(
				title: "Stopping Beam Services",
				message: "We need to stop the running services because the realm has changed. " +
				         "Please restart your services",
				ok: "Ok");
			}
			var _ = StopAll();
			async Promise StopAll(){
				_serviceToAction.Clear();
				var stopAllCommandArgs = new ProjectStopArgs
				{
					killTask = true
				};
				var command = _cli.ProjectStop(stopAllCommandArgs);
				command.OnStreamStopProjectCommandOutput(cb => { });
				await command.Run();
				await WaitReload();
			}
			
		}
	}
}
