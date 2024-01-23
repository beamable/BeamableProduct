using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System.Linq;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class DeployStandaloneMSLogParser : LoadingBarUpdater
	{
		private readonly string _serviceName;
		private readonly PublishService _publisher;
		public override string ProcessName => "Deploying...";

		private static readonly string[] globalSuccessLogs =
		{
			UPLOAD_CONTAINER_MESSAGE,
			CONTAINER_ALREADY_UPLOADED_MESSAGE
		};

		private static readonly string[] globalFailureLogs = { CANT_UPLOAD_CONTAINER_MESSAGE };

		private readonly string[] successLogs, failureLogs;

		public DeployStandaloneMSLogParser(ILoadingBar loadingBar, string name, PublishService publisher) : base(loadingBar)
		{
			_serviceName = name;
			_publisher = publisher;
			Step = 0;
			TotalSteps = 200;
			successLogs = globalSuccessLogs.Select(l => string.Format(l, _serviceName)).ToArray();
			failureLogs = globalFailureLogs.Select(l => string.Format(l, _serviceName)).ToArray();

			OnProgress(name, 0, 200);

			publisher.OnServiceDeployProgress += OnProgress;
			Application.logMessageReceived += HandleLog;
		}

		private void HandleLog(string logString, string stackTrace, LogType type)
		{
			if (successLogs.Contains(logString))
			{
				LoadingBar.SetUpdater(null);
				LoadingBar.UpdateProgress(1f);
			}
			else if (failureLogs.Contains(logString))
			{
				LoadingBar.SetUpdater(null);
				LoadingBar.UpdateProgress(0f, failed: true);
			}
		}

		private void OnProgress(string name, double buildProgress, double uploadProgress)
		{
			if (!name.Equals(_serviceName))
			{
				return;
			}

			Step = (int)(buildProgress + uploadProgress);
			LoadingBar.UpdateProgress((float)(uploadProgress + buildProgress) / 200);
		}

		protected override void OnKill()
		{
			_publisher.OnServiceDeployProgress -= OnProgress;
			Application.logMessageReceived -= HandleLog;
		}
	}
}
