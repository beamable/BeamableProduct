using Beamable.Common.Dependencies;
using Microsoft.Extensions.Logging;

namespace microservice.Observability;

public static class DependencyExtensions
{
    public static ILogger GetLogger<T>(this IDependencyProvider provider)
    {
        var factory = provider.GetService<ILoggerFactory>();
        return factory.CreateLogger<T>();
    }
}