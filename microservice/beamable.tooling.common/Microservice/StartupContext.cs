using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Server.Api.Usage;
using Beamable.Server.Common;
using Beamable.Server.Editor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;

namespace Beamable.Server;


public class StartupContext
{
    /// <summary>
    /// The arguments passed to the service, usually through environment variables. 
    /// </summary>
    public IMicroserviceArgs args;

    /// <summary>
    /// An array of types to use to find client callables
    /// </summary>
    public BeamRouteSource[] microserviceTypes;

    public IMicroserviceAttributes attributes;

    public string localEnvArgs;

    public BuiltSettings buildSettings;

    public List<Func<IDependencyProviderScope, Task>> initializers = new List<Func<IDependencyProviderScope, Task>>();

    public ReflectionCache reflectionCache;
    public IUsageApi ecsService;
    public string otlpEndpoint = null;
    public ILogger logger;
    public ILoggerFactory logFactory;
    
    public IActivityProvider activityProvider;
    public ResourceBuilder resourceProvider;
    public readonly BeamStandardTelemetryAttributeProvider standardBeamTelemetryAttributes = new BeamStandardTelemetryAttributeProvider();

    
    // public static List<BeamableMicroService> Instances = new List<BeamableMicroService>();

    /// <summary>
    /// True when the Microservice is starting inside a docker container. This uses the standard
    /// DOTNET_RUNNING_IN_CONTAINER environment variable that is set by the default dotnet container images. 
    /// </summary>
    public bool InDocker => args.InDocker();

    /// <summary>
    /// True when the Microservice is only starting to generate the open api document.
    /// </summary>
    public bool IsGeneratingOapi => args.ISGeneratingOapi();
}

public delegate ServiceMethodInstanceData BeamRouteTypeActivator(
    IMicroserviceArgs instanceArgs, 
    MicroserviceRequestContext requestContext);

public delegate ServiceMethodInstanceData<T> BeamRouteTypeActivator<T>(
    IMicroserviceArgs instanceArgs, 
    MicroserviceRequestContext requestContext)
    where T : class;

public class BeamRouteSource
{
    public BeamRouteTypeActivator Activator { get; set; }
    
    
    public Type InstanceType { get; set; }
    
    /// <summary>
    /// For example, the admin routes have admin/
    /// </summary>
    public string RoutePrefix { get; set; }
    
    /// <summary>
    /// The prefix that will be prepended to all auto-generated client methods from this route source.
    /// For example, if the method is "Add", and the prefix is "Tuna", the method name will be "Tuna_Add"
    /// </summary>
    public string ClientNamespacePrefix { get; set; }

}

public static class ArgExtensions
{
    /// <summary>
    /// True when the Microservice is starting inside a docker container. This uses the standard
    /// DOTNET_RUNNING_IN_CONTAINER environment variable that is set by the default dotnet container images. 
    /// </summary>
    public static bool InDocker(this IMicroserviceArgs _) => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    
    /// <summary>
    /// True when the Microservice is only starting to generate client code. 
    /// </summary>
    public static bool ISGeneratingOapi(this IMicroserviceArgs _) => Environment.GetCommandLineArgs().Contains("--generate-oapi");
}