
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.Environment;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.UI.Model
{
    [System.Serializable]
    public class MongoStorageModel : ServiceModelBase, IBeamableStorageObject
    {
        public StorageObjectDescriptor Descriptor { get; private set; }
        public MongoStorageBuilder Builder { get; private set; }
        public override IDescriptor GetDescriptor => Descriptor;
        public override bool IsRunning => Builder?.IsRunning ?? false;

        public override event Action<Task> OnStart;
        public override event Action<Task> OnStop;
        
        public static MongoStorageModel CreateNew(StorageObjectDescriptor descriptor)
        {
            return new MongoStorageModel
            {
                Descriptor = descriptor,
                Builder = Microservices.GetStorageBuilder(descriptor),
            };
        }

        public override Task Start()
        {
            OnStart?.Invoke(null);
            throw new NotImplementedException();
        }
        public override Task Stop()
        {
            OnStop?.Invoke(null);
            throw new NotImplementedException();
        }
        public override void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
        {
            throw new NotImplementedException();
        }
        private void OpenInCli()
        {
            throw new NotImplementedException();
        }
        public override void Refresh(IDescriptor descriptor)
        {
            // reset the descriptor and statemachines; because they aren't system.serializable durable.
            Descriptor = (StorageObjectDescriptor)descriptor;
            var oldBuilder = Builder;
            Builder = Microservices.GetStorageBuilder(Descriptor);
            Builder.ForwardEventsTo(oldBuilder);
        }
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
        private void CaptureLogs()
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