using System;
using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using UnityEngine.Experimental.UIElements;

namespace Beamable.Editor.UI.Model
{
    public abstract class ServiceModelBase : IBeamableService
    {
        public abstract bool IsRunning { get; }
        public bool AreLogsAttached { get; protected set; } = true;
        public LogMessageStore Logs { get; } = new LogMessageStore();
        
        public Action OnLogsDetached { get; set; }
        public Action OnLogsAttached { get; set; }
        public Action<bool> OnLogsAttachmentChanged { get; set; }
        public Action<bool> OnSelectionChanged { get; set; }
        public Action OnSortChanged { get; set; }

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
        public abstract void EnrichWithRemoteReference(ServiceReference remoteReference);
        public abstract void EnrichWithStatus(ServiceStatus status);
        public abstract void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
        // TODO === END

        public ServiceType GetServiceType() => GetDescriptor().ServiceType;
        public LogMessageStore GetLogs() => Logs;
        public abstract IDescriptor GetDescriptor();
        public abstract void Refresh(IDescriptor descriptor);
        public abstract Task Start();
        public abstract Task Stop();
    }
}