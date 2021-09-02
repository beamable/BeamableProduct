using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI.Components {
    public class DeployLogParser : LoadingBarUpdater {
        public override string ProcessName => "Deploying";
        private readonly MicroserviceRecord[] _records;

        public DeployLogParser(ILoadingBar loadingBar, ManifestModel model) : base(loadingBar) {
            _records = model.Services.Values
                .Select(m => new MicroserviceRecord(m.ServiceName))
                .ToArray();
            Application.logMessageReceived += HandleLog;
            Update();
        }
        
        void HandleLog(string logString, string stackTrace, LogType type)
        {
            foreach (var record in _records) {
                if (logString == string.Format(BeamableLogConstants.UploadedContainerMessage, record.name)
                    || logString == string.Format(BeamableLogConstants.ContainerAlreadyUploadedMessage, record.name)) {
                    record.deployed = true;
                    Update();
                    return;
                }
            }
        }

        private void Update() {
            if (_records.All(r => r.deployed)) {
                Succeeded = true;
                _loadingBar.UpdateProgress(1f, $"(Deployed)");
                Kill();
            }
            else {
                Step = _records.Count(r => r.deployed);
                TotalSteps = _records.Length;
                _loadingBar.UpdateProgress(Step / (float) TotalSteps, $"({ProcessName} {StepText})");
            }
        }
        
        protected override void OnKill() {
            Application.logMessageReceived -= HandleLog;
        }

        private class MicroserviceRecord {
            public readonly string name;
            public bool deployed;
            
            public MicroserviceRecord(string name) {
                this.name = name;
            }
        }
    }
}