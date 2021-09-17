using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public class RunImageLogParser : UniversalLogsParser {
        private static readonly string[] expectedLogs = new[] {
            "Starting...",
            "Scanning client methods",
            "Registering standard services",
            "Registering custom services",
            "Service provider initialized",
            "Event provider initialized"
        };

        private static readonly string[] errorElements = new[] {
            "Error",
            "Exception",
            "exception"
        };

        public override string StepText => $"(Starting {base.StepText} MS {_model.Descriptor.Name})";
        public override string ProcessName => $"Starting MS {_model?.Descriptor?.Name}";

        public RunImageLogParser(ILoadingBar loadingBar, MicroserviceModel model) : base(loadingBar, model) {
            TotalSteps = expectedLogs.Length;
            LoadingBar.UpdateProgress(0f, $"({ProcessName})");
        }
        public override bool DetectSuccess(string message) {
            return message.StartsWith("Service ready for traffic.");
        }

        public override bool DetectFailure(string message) {
            return errorElements.Any(message.Contains);
        }

        public override bool DetectStep(string message, out int step) {
            step = 0;
            for (int i = 0; i < TotalSteps; i++) {
                if (message.StartsWith(expectedLogs[i])) {
                    step = i + 1;
                    return true;
                }
            }

            return false;
        }
    }
}