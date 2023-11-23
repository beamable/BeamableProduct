using Beamable.Common;
using Beamable.Server.Editor.Usam;
using System;
using System.Threading.Tasks;

namespace Beamable.Editor.UI.Model
{
	public class BeamoServiceBuilder : IBeamableBuilder
	{
		public Task CheckIfIsRunning()
		{
			return Task.CompletedTask;
		}

		public Task TryToStart()
		{
			return CodeService.Run(new[] { BeamoId }).TaskFromPromise();
		}

		public Task TryToStop()
		{
			return CodeService.Stop(new[] { BeamoId }).TaskFromPromise();
		}

		public Task TryToRestart()
		{
			return CodeService.Stop(new[] { BeamoId })
							  .Map(_ => CodeService.Run(new[] { BeamoId })).TaskFromPromise();
		}
		public string BeamoId { get; set; }
		public Action<bool> OnIsRunningChanged { get; set; }
		public Action<int, int> OnBuildingProgress { get; set; }
		public Action<int, int> OnStartingProgress { get; set; }
		public Action<bool> OnStartingFinished { get; set; }
		public Action<bool> OnBuildingFinished { get; set; }
		public bool IsRunning { get; set; }
		private CodeService CodeService => BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
	}
}
