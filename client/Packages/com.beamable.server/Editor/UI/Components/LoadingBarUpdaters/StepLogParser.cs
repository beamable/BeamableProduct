using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public class StepLogParser : LoadingBarUpdater {
        private static readonly Regex _StepRegex = new Regex("Step [0-9]+/[0-9]+");
        private static readonly Regex _NumberRegex = new Regex("[0-9]+");
        private readonly ServiceModelBase _model;
        private readonly Task _task;

        public override string StepText => $"(Building {base.StepText} MS {_model.Name})";
        public override string ProcessName => $"Building MS {_model?.Descriptor?.Name}";

        private static readonly string[] errorElements = new[] {
            "error",
            "Error",
            "Exception",
            "exception"
        };

        public StepLogParser(ILoadingBar loadingBar, ServiceModelBase model, Task task) : base(loadingBar) {
            _model = model;
            _task = task;

            LoadingBar.UpdateProgress(0f, $"({ProcessName})");

            _model.Logs.OnMessagesUpdated += OnMessagesUpdated;
            task?.ContinueWith(_ => Kill());
        }

        protected override void OnKill() {
            if (_task?.IsFaulted ?? false) {
                GotError = true;
                LoadingBar.UpdateProgress(0f, "(Error)", true);
            }
            _model.Logs.OnMessagesUpdated -= OnMessagesUpdated;
        }

        private void OnMessagesUpdated() {
            var message = _model.Logs.Messages.LastOrDefault()?.Message;
            if (string.IsNullOrWhiteSpace(message)) return;
            var match = _StepRegex.Match(message);
            if (match.Success) {
                var values = _NumberRegex.Matches(match.Value);
                var current = int.Parse(values[0].Value);
                var total = int.Parse(values[1].Value);
                Step = current;
                TotalSteps = total;
                LoadingBar.UpdateProgress((current - 1f) / total, match.Value);
            }
            else if (message.Contains("Success")) {
                Succeeded = true;
                LoadingBar.UpdateProgress(1f, "(Success)");
            }
            else if (errorElements.Any(message.Contains)) { // TODO: check task exit code to detect errors
                GotError = true;
                LoadingBar.UpdateProgress(0f, "(Error)",true);
                Kill();
            }
        }
    }
}