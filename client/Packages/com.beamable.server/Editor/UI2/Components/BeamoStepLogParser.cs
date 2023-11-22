using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.Editor.Microservice.UI2.Components
{
	public class BeamoStepLogParser : LoadingBarUpdater
	{
		private readonly IBeamableBuilder _builder;
		private readonly string _name;
		private readonly Task _task;

		public override string StepText => $"(Running {base.StepText} MS {_name})";
		public override string ProcessName => $"Running MS {_name}";

		public BeamoStepLogParser(ILoadingBar loadingBar, IBeamableBuilder builder, string name) :
			base(loadingBar)
		{
			_builder = builder;
			_name = name;
			LoadingBar.UpdateProgress(0f, $"({ProcessName})");
			
			_builder.OnStartingProgress += HandleBuildingProgress;
			_builder.OnStartingFinished += HandleBuildingFinished;
		}

		private void HandleBuildingProgress(int currentStep, int totalSteps)
		{
			if (currentStep == 0)
				return;
			Step = currentStep;
			TotalSteps = totalSteps;
			LoadingBar.UpdateProgress((currentStep - 1f) / totalSteps, StepText);
		}

		private void HandleBuildingFinished(bool success)
		{
			var value = success ? 1.0f : 0.0f;
			var message = success ? "(Success)" : "(Error)";
			LoadingBar.UpdateProgress(value, message, !success);
			if (success)
			{
				Succeeded = true;
			}
			else
			{
				GotError = true;
				Kill();
			}
		}

		protected override void OnKill()
		{
			if (_task?.IsFaulted ?? false)
			{
				GotError = true;
				LoadingBar.UpdateProgress(0f, "(Error)", true);
			}

			_builder.OnBuildingFinished -= HandleBuildingFinished;
			_builder.OnBuildingProgress -= HandleBuildingProgress;
		}
	}
}
