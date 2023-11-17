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
			return BeamableTaskExtensions.TaskFromPromise(CodeService.Run(new[] {BeamoId}));
		}

		public Task TryToStop()
		{
			return BeamableTaskExtensions.TaskFromPromise(CodeService.Stop(new[] {BeamoId}));
		}

		public Task TryToRestart()
		{
			return BeamableTaskExtensions.TaskFromPromise(
				CodeService.Run(new[] {BeamoId})
				           .Map(_=>CodeService.Run(new[] {BeamoId})));
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
