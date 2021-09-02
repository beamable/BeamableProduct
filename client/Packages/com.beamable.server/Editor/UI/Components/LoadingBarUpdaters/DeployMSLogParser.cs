using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public class DeployMSLogParser : LoadingBarUpdater {
        private readonly MicroserviceModel _model;
        public override string ProcessName { get; } = "Deploying...";

        private static readonly string[] globalSuccessLogs = new[] {
            BeamableLogConstants.UploadedContainerMessage,
            BeamableLogConstants.ContainerAlreadyUploadedMessage
        };

        private readonly string[] successLogs;

        public DeployMSLogParser(ILoadingBar loadingBar, MicroserviceModel model) : base(loadingBar) {
            _model = model;
            Step = 0;
            TotalSteps = 1;
            successLogs = globalSuccessLogs
                .Select(l => string.Format(l, model.Name)).ToArray();
            OnProgress(0);
            //_model.OnDeployProgress += OnProgress;
            Application.logMessageReceived += HandleLog;
            _model.Logs.OnMessagesUpdated += OnMessagesUpdated;
        }

        private void OnMessagesUpdated() {
            var message = _model.Logs.Messages.LastOrDefault()?.Message;
            if (string.IsNullOrWhiteSpace(message)) return;
            
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (successLogs.Contains(logString)) {
                _loadingBar.UpdateProgress(1f, $"(Deployed)");
                Kill();
            }
        }

        private void OnProgress(float progress) {
            _loadingBar.UpdateProgress(progress, $"({ProcessName})");
        }
        
        protected override void OnKill() {
            _model.OnDeployProgress -= OnProgress;
            Application.logMessageReceived -= HandleLog;
            _model.Logs.OnMessagesUpdated -= OnMessagesUpdated;
        }
    }
}