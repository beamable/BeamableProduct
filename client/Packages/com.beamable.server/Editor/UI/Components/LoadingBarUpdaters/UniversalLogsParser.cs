using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public abstract class UniversalLogsParser : LoadingBarUpdater {
        protected readonly MicroserviceModel _model;

        public UniversalLogsParser(ILoadingBar loadingBar, MicroserviceModel model) : base(loadingBar) {
            _model = model;
            _model.Logs.OnMessagesUpdated += OnMessagesUpdated;
        }
        protected override void OnKill() {
            _model.Logs.OnMessagesUpdated -= OnMessagesUpdated;
        }

        private void OnMessagesUpdated() {
            var message = _model.Logs.Messages.LastOrDefault()?.Message;
            if (string.IsNullOrWhiteSpace(message)) return;

            if (DetectSuccess(message)) {
                Succeeded = true;
                _loadingBar.UpdateProgress(1f, $"(Success: {ProcessName})");
                Kill();
            }
            else if (DetectFailure(message)) {
                GotError = true;
                _loadingBar.UpdateProgress(0f, $"(Error: {ProcessName})", true);
                Kill();
            }
            else if (DetectStep(message, out var step)) {
                Step = step;
                _loadingBar.UpdateProgress((Step - 1f) / TotalSteps, StepText);
            }
        }
        public abstract bool DetectSuccess(string message);
        public abstract bool DetectFailure(string message);
        public abstract bool DetectStep(string message, out int step);
    }
}