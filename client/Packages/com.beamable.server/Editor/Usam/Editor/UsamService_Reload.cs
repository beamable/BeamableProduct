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
					service.IsReadonlyPackage = BeamManifestServiceEntry.IsReadonlyProject(service.csprojPath);
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
