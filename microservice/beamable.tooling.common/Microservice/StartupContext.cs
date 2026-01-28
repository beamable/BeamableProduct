using System.Text;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
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
    public BeamRouteSource[] routeSources;

    public IMicroserviceAttributes attributes;

    [Obsolete("Use the " + nameof(generateLocalEnvInvocationModifier) + " instead")]
    public string localEnvArgs;

    public List<Func<IDependencyProviderScope, Task>> initializers = new List<Func<IDependencyProviderScope, Task>>();

    public ReflectionCache reflectionCache;
    public IUsageApi ecsService;
    public string otlpEndpoint = null;
    public ILogger logger;
    public ILoggerFactory logFactory;
    public IActivityProvider activityProvider;
    public ResourceBuilder resourceProvider;
    public DebugLogProcessor debugLogProcessor;
    public MicroserviceResult result = new MicroserviceResult();
    public List<IBeamableService> services = new List<IBeamableService>();
    public readonly BeamStandardTelemetryAttributeProvider standardBeamTelemetryAttributes = new BeamStandardTelemetryAttributeProvider();
    public IDependencyBuilder serviceBuilder;
    public Action<BeamCliInvocation> generateLocalEnvInvocationModifier;


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

public class MicroserviceResult
{
    public bool GeneratedClient { get; set; }
    public bool Success { get; set; }
}

public struct BeamCliPart
{
    public string key;
    public OptionalString value;

    public BeamCliPart(string key)
    {
        this.key = key;
        value = OptionalString.None;
    }

    public BeamCliPart(string key, string value)
    {
        this.key = key;
        this.value = OptionalString.FromString(value);
    }
}

public class BeamCliInvocation
{
    public string command;
    public List<BeamCliPart> args;

    public void AddArgIfNotExist(BeamCliPart arg)
    {
        if (args.Any(x => string.Equals(x.key, arg.key, StringComparison.InvariantCultureIgnoreCase)))
            return;
        args.Add(arg);
    }
    
    public string ToCommandString()
    {
        var sb = new StringBuilder();
        ApplyCommandString(sb);
        return sb.ToString();
    }
    public void ApplyCommandString(StringBuilder sb)
    {
        sb.Append(command);
        for (var i = 0; i < args.Count; i++)
        {
            sb.Append(" ");
            sb.Append(args[i].key);
            if (args[i].value.TryGet(out var val))
            {
                sb.Append(" ");
                sb.Append(val);
            }
        }
    }

    public void RemoveKey(string key)
    {
        args.RemoveAll(x =>
            string.Equals(key, x.key, StringComparison.InvariantCultureIgnoreCase));
    }
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
    
    
    /// <summary>
    /// The routing key is the old name prefix.
    /// There is no routing key when the service is deployed.
    /// </summary>
    /// <returns>false if there is no routing key</returns>
    public static bool TryGetRoutingKey(this IMicroserviceArgs args, out string routingKey)
    {
        routingKey = args.NamePrefix;
        return !string.IsNullOrEmpty(routingKey);
    }

    public static OptionalString GetRoutingKey(this IMicroserviceArgs args)
    {
        return OptionalString.FromString(args.NamePrefix);
    }

}