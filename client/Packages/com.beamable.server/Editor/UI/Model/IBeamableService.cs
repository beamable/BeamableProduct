using System;
using Beamable.Server.Editor;

namespace Beamable.Editor.UI.Model
{
    public interface IBeamableService : IEquatable<IDescriptor>
    {
        ServiceType GetServiceType();
        IDescriptor GetDescriptor();
        LogMessageStore GetLogs();
        void Refresh(IDescriptor descriptor);
    }
}