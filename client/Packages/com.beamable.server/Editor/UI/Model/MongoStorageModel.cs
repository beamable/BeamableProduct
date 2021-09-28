using System;
using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;

namespace Beamable.Editor.UI.Model
{
    [System.Serializable]
    public class MongoStorageModel : IBeamableService
    {
        public StorageObjectDescriptor Descriptor;
        public LogMessageStore Logs;
        public MongoStorageBuilder Builder;
        public bool IsRunning => Builder?.IsRunning ?? false;
        public string Name => Descriptor.Name;
        public IDescriptor GetDescriptor() => Descriptor;
        public LogMessageStore GetLogs() => Logs;
        
        public void Refresh(IDescriptor descriptor)
        {
            // reset the descriptor and statemachines; because they aren't system.serializable durable.
            Descriptor = (StorageObjectDescriptor)descriptor;
            var oldBuilder = Builder;
            Builder = Microservices.GetStorageBuilder(Descriptor);
            Builder.ForwardEventsTo(oldBuilder);
        }

        public static MongoStorageModel CreateNew(StorageObjectDescriptor descriptor)
        {
            return new MongoStorageModel
            {
                Descriptor = descriptor,
                Builder = Microservices.GetStorageBuilder( descriptor),
                Logs = new LogMessageStore()
            };
        }

        public MicroserviceEditor.ServiceType GetServiceType() => MicroserviceEditor.ServiceType.StorageObject;
        public bool Equals(IDescriptor other) => GetDescriptor().Name.Equals(other?.Name);
        
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