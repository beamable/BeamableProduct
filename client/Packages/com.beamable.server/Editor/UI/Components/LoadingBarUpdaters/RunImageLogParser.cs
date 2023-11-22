using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.DockerCommands;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class RunImageLogParser : LoadingBarUpdater
	{
		private readonly IBeamableBuilder _builder;
		private readonly string _name;

		public override string StepText => $"(Starting {base.StepText} MS {_name})";
		public override string ProcessName => $"Starting MS {_name}";
		protected override void OnKill()
		{
			_builder.OnStartingFinished -= HandleStartingFinished;
			_builder.OnStartingProgress -= HandleStartingProgress;
		}

		public RunImageLogParser(ILoadingBar loadingBar, IBeamableBuilder builder, string name) : base(loadingBar)
		{
			_builder = builder;
			_name = name;
			TotalSteps = MicroserviceLogHelper.RunLogsSteps;
			LoadingBar.UpdateProgress(0f, $"({ProcessName})");
			_builder.OnStartingFinished += HandleStartingFinished;
			_builder.OnStartingProgress += HandleStartingProgress;
		}

		private void HandleStartingFinished(bool success)
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
			}
			Kill();
		}

		private void HandleStartingProgress(int currentStep, int totalSteps)
		{
			Step = currentStep;
			TotalSteps = totalSteps;
			LoadingBar.UpdateProgress((currentStep - 1f) / totalSteps, StepText);
		}
	}
}
