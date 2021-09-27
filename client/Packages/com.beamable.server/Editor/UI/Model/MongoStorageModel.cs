using System;
using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;

namespace Beamable.Editor.UI.Model
{
    [System.Serializable]
    public class MongoStorageModel
    {
        public StorageObjectDescriptor Descriptor;
        public LogMessageStore Logs;
        public MongoStorageBuilder Builder;
        public bool IsRunning => Builder?.IsRunning ?? false;
        public string Name => Descriptor.Name;
    }

    public class MongoStorageBuilder
    {
        public Action<bool> OnIsRunningChanged;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (value == _isRunning) return;
                _isRunning = value;
                // XXX: If OnIsRunningChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
                EditorApplication.delayCall += () => OnIsRunningChanged?.Invoke(value);
            }
        }
        public StorageObjectDescriptor Descriptor { get; private set; }

        private DockerCommand _logProcess, _runProcess;
        private bool _isRunning;
        
        public async void Init(StorageObjectDescriptor descriptor)
        {
            Descriptor = descriptor;

            _isRunning = false;
            await CheckIfIsRunning();
            if (IsRunning)
            {
                CaptureLogs();
            }
        }
        void CaptureLogs()
        {
            _logProcess?.Kill();
            _logProcess = new FollowLogCommand(Descriptor);
            _logProcess.Start();
        }
        
        public async Task CheckIfIsRunning()
        {
            var checkProcess = new CheckImageReturnableCommand(Descriptor)
            {
                WriteLogToUnity = false, WriteCommandToUnity = false
            };

            _isRunning = await checkProcess.Start(null);
        }
        
        public void ForwardEventsTo(MongoStorageBuilder oldBuilder)
        {
            if (oldBuilder == null) return;
            OnIsRunningChanged += oldBuilder.OnIsRunningChanged;
        }
    }
}