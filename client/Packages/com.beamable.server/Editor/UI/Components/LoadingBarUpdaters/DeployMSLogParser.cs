using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public class DeployMSLogParser : LoadingBarUpdater {
        private readonly ServiceModelBase _model;
        private readonly bool _showName;
        public override string ProcessName { get; } = "Deploying...";

        private static readonly string[] globalSuccessLogs = new[] {
            BeamableLogConstants.UploadedContainerMessage,
            BeamableLogConstants.ContainerAlreadyUploadedMessage
        };

        private static readonly string[] globalFailureLogs = new[] {
            BeamableLogConstants.CantUploadContainerMessage
        };

        private readonly string[] successLogs, failureLogs;

        public DeployMSLogParser(ILoadingBar loadingBar, ServiceModelBase model, bool showName = false) : base(loadingBar) {
            _model = model;
            _showName = showName;
            Step = 0;
            TotalSteps = 1;
            successLogs = globalSuccessLogs
                .Select(l => string.Format(l, model.Name)).ToArray();

            failureLogs = globalFailureLogs
                .Select(l => string.Format(l, model.Name)).ToArray();

            OnProgress(0, 0, 1);
            _model.OnDeployProgress += OnProgress;
            Application.logMessageReceived += HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (successLogs.Contains(logString)) {
                LoadingBar.UpdateProgress(1f, _showName ? $"{_model.Name}: Deployed" : $"(Deployed)");
                Kill();
            }
            else if (failureLogs.Contains(logString)) {
                LoadingBar.UpdateProgress(0f, $"(Error: {ProcessName})", true);
                Kill();
            }
        }

        private void OnProgress(float progress, long step, long total)
        {
            TotalSteps = (int) total;
            Step = (int) step;
            LoadingBar.UpdateProgress(progress, _showName ? $"{_model.Name}: {ProcessName}" :$"({ProcessName})");
        }

        protected override void OnKill() {
            _model.OnDeployProgress -= OnProgress;
            Application.logMessageReceived -= HandleLog;
        }
    }
}