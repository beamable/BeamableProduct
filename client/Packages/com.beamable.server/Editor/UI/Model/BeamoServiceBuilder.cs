using Beamable.Common;
using Beamable.Server.Editor;
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

		public Promise TryToStart()
		{
			return CodeService.RunStandaloneMicroservice(BeamoId);
		}

		public Promise TryToStop()
		{
			return CodeService.StopStandaloneMicroservice(new[] { BeamoId });
		}

		public Promise TryToRestart()
		{
			return CodeService.StopStandaloneMicroservice(new[] { BeamoId })
							  .Then(p =>
							  {
								  _ = CodeService.RunStandaloneMicroservice(BeamoId);
							  }).ToPromise();
		}
		public string BeamoId { get; set; }
		public ServiceType ServiceType { get; set; }
		public Action<bool> OnIsRunningChanged { get; set; }
		public Action<int, int> OnBuildingProgress { get; set; }
		public Action<int, int> OnStartingProgress { get; set; }
		public Action<bool> OnStartingFinished { get; set; }
		public Action<bool> OnBuildingFinished { get; set; }
		public bool IsRunning
		{
			get => _isRunning;
			set
			{
				// if (value == _isRunning) return;
				_isRunning = value;
				BeamEditorContext.Default.Dispatcher.Schedule(() => OnIsRunningChanged?.Invoke(value));
			}
		}

		private CodeService CodeService => BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
		private bool _isRunning;
	}
}
