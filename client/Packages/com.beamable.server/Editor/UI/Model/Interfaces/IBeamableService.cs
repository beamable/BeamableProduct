using System;
using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using UnityEngine.Experimental.UIElements;

namespace Beamable.Editor.UI.Model
{
    public interface IBeamableService
    {
        bool IsRunning { get; }
        bool AreLogsAttached { get; }
        LogMessageStore Logs { get; }

        Action OnLogsDetached { get; set; }
        Action OnLogsAttached { get; set; }
        Action<bool> OnLogsAttachmentChanged { get; set; }
        Action<bool> OnSelectionChanged { get; set; }
        Action OnSortChanged { get; set; }
        
        event Action<Task> OnStart;
        event Action<Task> OnStop;

        ServiceType GetServiceType();
        IDescriptor GetDescriptor();
        LogMessageStore GetLogs();
        void Refresh(IDescriptor descriptor);

        void DetachLogs();
        void AttachLogs();
        void EnrichWithRemoteReference(ServiceReference remoteReference);
        void EnrichWithStatus(ServiceStatus status);
        void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
        Task Start();
        Task Stop();

    }
}