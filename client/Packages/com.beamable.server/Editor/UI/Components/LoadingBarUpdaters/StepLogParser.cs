using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class StepLogParser : LoadingBarUpdater
	{
		private readonly IBeamableBuilder _builder;
		private readonly string _name;
		private readonly Task _task;

		public override string StepText => $"(Building {base.StepText} MS {_name})";
		public override string ProcessName => $"Building MS {_name}";

		public StepLogParser(ILoadingBar loadingBar, IBeamableBuilder builder,string name, Task task) : base(loadingBar)
		{
			_builder = builder;
			_name = name;
			_task = task;

			LoadingBar.UpdateProgress(0f, $"({ProcessName})");

			_builder.OnBuildingFinished += HandleBuildingFinished;
			_builder.OnBuildingProgress += HandleBuildingProgress;
			task?.ContinueWith(_ => Kill());
		}

		private void HandleBuildingProgress(int currentStep, int totalSteps)
		{
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
