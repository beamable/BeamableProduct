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
    public abstract class ServiceModelBase : IBeamableService
    {
        public abstract bool IsRunning { get; }
        public bool AreLogsAttached
        {
            get => _areLogsAttached;
            protected set => _areLogsAttached = value;
        }

        [SerializeField] private bool _areLogsAttached = true;
        [SerializeField] private LogMessageStore _logs = new LogMessageStore();

        public LogMessageStore Logs => _logs;

        public abstract IDescriptor Descriptor { get; }
        public abstract IBeamableBuilder Builder { get; }
        public ServiceType ServiceType => Descriptor.ServiceType;
        public string Name => Descriptor.Name;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnSelectionChanged?.Invoke(value);
            }
        }
        private bool _isSelected;

        public Action OnLogsDetached { get; set; }
        public Action OnLogsAttached { get; set; }
        public Action<bool> OnLogsAttachmentChanged { get; set; }
        public Action<bool> OnSelectionChanged { get; set; }
        public Action OnSortChanged { get; set; }
        public Action<float, long, long> OnDeployProgress { get; set; }

        public abstract event Action<Task> OnStart;
        public abstract event Action<Task> OnStop;

        public void DetachLogs()
        {
            if (!AreLogsAttached) return;

            AreLogsAttached = false;
            OnLogsDetached?.Invoke();
            OnLogsAttachmentChanged?.Invoke(false);
        }
        public void AttachLogs()
        {
            if (AreLogsAttached) return;
            AreLogsAttached = true;
            OnLogsAttached?.Invoke();
            OnLogsAttachmentChanged?.Invoke(true);
        }
        
        // TODO - When MongoStorageModel will be ready feel free to implement these methods
        // TODO === BEGIN
        public abstract void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
        // TODO === END
        public abstract void Refresh(IDescriptor descriptor);
        public abstract Task Start();
        public abstract Task Stop();
        
        protected void OpenCode()
        {
            var path = Path.GetDirectoryName(AssemblyDefinitionHelper.ConvertToInfo(Descriptor).Location);
            EditorUtility.OpenWithDefaultApp($@"{path}/{Descriptor.Name}.cs");
        }
    }
}