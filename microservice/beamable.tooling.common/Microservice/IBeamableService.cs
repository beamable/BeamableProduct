using Beamable.Common.Dependencies;

namespace Beamable.Server;

public interface IBeamableService
{
    SocketRequesterContext SocketContext { get; }
    Task OnShutdown(object sender, EventArgs args);
    bool HasInitialized { get; }
    IDependencyProvider Provider { get; }
}