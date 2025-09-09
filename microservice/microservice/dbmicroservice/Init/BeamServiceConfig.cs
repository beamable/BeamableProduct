using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using Beamable.Server.Content;
using beamable.tooling.common.Microservice;
using microservice.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Beamable.Server;

public class MicroserviceResult
{
	public bool GeneratedClient { get; set; }
	public List<BeamableMicroService> Instances { get; set; } = new List<BeamableMicroService>();
}


public static partial class MicroserviceBootstrapper
{
    public static async Task<MicroserviceResult> Begin(IBeamServiceConfig configurator)
    {
	    // de-duplicate any route information.
	    configurator.RouteSource = configurator.RouteSource.DistinctBy(d => d.InstanceType).ToList();
	    
	    var result = new MicroserviceResult();
	    var configuredArgs = configurator.Args.Copy();
	    
	    var startupCtx = new StartupContext
	    {
		    args = configuredArgs,
		    microserviceTypes = configurator.RouteSource.ToArray(),
		    attributes = configurator.Attributes,
		    localEnvArgs = configurator.LocalEnvCustomArgs,
		    initializers = configurator.ServiceInitializers.ToList()
	    };

	    if (startupCtx.IsGeneratingOapi)
	    {
		    await GenerateOpenApiSpecification(startupCtx);
		    result.GeneratedClient = true;
		    return result;
	    }

	    ConfigureLogging(configurator, startupCtx, includeOtel: false, string.Empty, out var debugLogProcessor);

	    startupCtx.logger.LogInformation($"Starting Prepare");

	    if (!startupCtx.args.SkipLocalEnv)
	    {
		    await GetLocalEnvironment(startupCtx);
		    var freshEnvArgs = new EnvironmentArgs();
		    configuredArgs.Host = freshEnvArgs.Host;
		    configuredArgs.CustomerID = freshEnvArgs.CustomerID;
		    configuredArgs.ProjectName = freshEnvArgs.ProjectName;
		    configuredArgs.Secret = freshEnvArgs.Secret;
		    configuredArgs.NamePrefix = freshEnvArgs.NamePrefix;
		    configuredArgs.BeamInstanceCount = freshEnvArgs.BeamInstanceCount;
		    configuredArgs.RefreshToken = freshEnvArgs.RefreshToken;
		    configuredArgs.AccountId = freshEnvArgs.AccountId;
	    }

	    ConfigureRequiredProcessIdWatcher(startupCtx);

	    await ConfigureOtelCollector(startupCtx);

	    ConfigureOtelData(startupCtx);
	    ConfigureLogging(configurator, startupCtx, includeOtel: true, otlpEndpoint: startupCtx.otlpEndpoint, out debugLogProcessor);
	    ConfigureTelemetry(startupCtx);


	    ConfigureUncaughtExceptions(startupCtx);
	    ConfigureUnhandledError();
	    _ = ConfigureDiscovery(startupCtx);
	    await ConfigureUsageService(startupCtx);
	    startupCtx.reflectionCache = ConfigureReflectionCache(startupCtx);

	    // configure the root service scope, and then build the root service provider.
	    var serviceBuilder = ConfigureServices(startupCtx, result);
	    foreach (var serviceConfiguration in configurator.ServiceConfigurations)
	    {
		    // TODO: try/catch
		    serviceConfiguration(serviceBuilder);
	    }
	    
	    var rootServiceScope = serviceBuilder.Build(new BuildOptions
	    {
		    allowHydration = false
	    });

	    InitializeServices(rootServiceScope);
	    rootServiceScope.GetService<FederationMetadata>().Components =
		    FederatedComponentGenerator.FindFederatedComponents(startupCtx);


	    var resolvedCid = await ConfigureCid(startupCtx.args);
	    var args = startupCtx.args.Copy(conf =>
	    {
		    conf.ServiceScope = rootServiceScope;
		    conf.CustomerID = resolvedCid;
	    });
	    
	    for (var i = 0; i < args.BeamInstanceCount; i++)
	    {
		    var isFirstInstance = i == 0;
		    var beamableService = new BeamableMicroService();
		    result.Instances.Add(beamableService);
		    // Instances.Add(beamableService);

		    var instanceArgs = args.Copy(conf =>
		    {
			    // only the first instance needs to run, if anything should run at all.
			    conf.DisableCustomInitializationHooks |= !isFirstInstance;
		    });

		    if (isFirstInstance)
		    {
			    var localDebug = new ContainerDiagnosticService(instanceArgs, beamableService, debugLogProcessor);
			    var runningDebugTask = localDebug.Run();
		    }

		    //In case that SdkVersionExecution is null or empty, we are executing it locally with dotnet and
		    //therefore getting dependencies through nuget, so not required to check versions mismatch.
		    if (!string.IsNullOrEmpty(args.SdkVersionExecution) &&
		        !string.Equals(args.SdkVersionExecution, args.SdkVersionBaseBuild))
		    {
			    startupCtx.logger.ZLogCritical(
				    $"Version mismatch. Image built with {args.SdkVersionBaseBuild}, but is executing with {args.SdkVersionExecution}. This is a fatal mistake.");
			    throw new Exception(
				    $"Version mismatch. Image built with {args.SdkVersionBaseBuild}, but is executing with {args.SdkVersionExecution}. This is a fatal mistake.");
		    }

		    try
		    {
			    await beamableService.Start(instanceArgs, startupCtx);
			    if (isFirstInstance && (startupCtx.attributes?.EnableEagerContentLoading ?? false))
			    {
				    await rootServiceScope.GetService<ContentService>().initializedPromise;
			    }
		    }
		    catch (Exception ex)
		    {
			    var message = new StringBuilder(1024 * 10);

			    if (ex is not BeamableMicroserviceException beamEx)
				    message.AppendLine(
					    $"[BeamErrorCode=BMS{BeamableMicroserviceException.kBMS_UNHANDLED_EXCEPTION_ERROR_CODE}]" +
					    $" Unhandled Exception Found! Please notify Beamable of your use case that led to this.");
			    else
				    message.AppendLine($"[BeamErrorCode=BMS{beamEx.ErrorCode}] " +
				                       $"Beamable Exception Found! If the message is unclear, please contact Beamable with your feedback.");

			    message.AppendLine("Exception Info:");
			    message.AppendLine($"Name={ex.GetType().Name}, Message={ex.Message}");
			    message.AppendLine("Stack Trace:");
			    message.AppendLine(ex.StackTrace);
			    startupCtx.logger.LogCritical(message.ToString());
			    throw;
		    }

		    var _ = beamableService.RunForever();
	    }

	    return result;
	    // await Task.Delay(-1);

    }
}

public class BeamServiceConfig : IBeamServiceConfig
{
	IMicroserviceArgs IBeamServiceConfig.Args { get; set; } = new EnvironmentArgs();
    IMicroserviceAttributes IBeamServiceConfig.Attributes { get; set; }
    List<BeamRouteSource> IBeamServiceConfig.RouteSource { get; set; } = new List<BeamRouteSource>();
    
    string IBeamServiceConfig.LocalEnvCustomArgs { get; set; }
    List<Action<IDependencyBuilder>> IBeamServiceConfig.ServiceConfigurations { get; set; } = new List<Action<IDependencyBuilder>>();

    List<Func<IDependencyProviderScope, Task>> IBeamServiceConfig.ServiceInitializers { get; set; } = new List<Func<IDependencyProviderScope, Task>>();
    Func<ILogger> IBeamServiceConfig.LogFactory { get; set; }
}

public interface IBeamServiceConfig
{
    IMicroserviceArgs Args { get; set; }
    string LocalEnvCustomArgs { get; set; }
    IMicroserviceAttributes Attributes { get; set; }
    List<BeamRouteSource> RouteSource { get; set; }
    List<Action<IDependencyBuilder>> ServiceConfigurations { get; set; }
    List<Func<IDependencyProviderScope, Task>> ServiceInitializers { get; set; }
    Func<ILogger> LogFactory { get; set; }
}

public static class BeamServer
{
	public static BeamServiceConfigBuilder Create() => new BeamServiceConfigBuilder();
}


public class BeamServiceConfigBuilder
{
    public BeamServiceConfig Config { get; set; }

    public BeamServiceConfigBuilder(IMicroserviceAttributes attributes=null)
    {
	    Config = new BeamServiceConfig();
	    var conf = Config as IBeamServiceConfig;
	    conf.Args = new EnvironmentArgs();

	    conf.Attributes = attributes ?? BuiltSettings.ReadServiceAttributes();
	    WithAdminRoutes();
    }

    private BeamServiceConfigBuilder WithAdminRoutes()
    {
	    var conf = Config as IBeamServiceConfig;
	    AdminRoutes instance = null;
	    
	    return IncludeRoutes<AdminRoutes>("admin/", null, (args, reqCtx) =>
	    {
		    if (instance == null)
		    {
			    instance = new AdminRoutes
			    {
				    sdkVersionBaseBuild = args.SdkVersionBaseBuild,
				    sdkVersionExecution = args.SdkVersionExecution,
				    GlobalProvider = args.ServiceScope,
				    FederationComponents = args.ServiceScope.GetService<FederationMetadata>().Components,
				    MicroserviceAttribute = conf.Attributes, 
				    routingKey = args.NamePrefix,
				    PublicHost = $"{args.Host.Replace("wss://", "https://").Replace("/socket", "")}/basic/{args.CustomerID}.{args.ProjectName}.{conf.Attributes.GetQualifiedName()}/"
			    };
		    }

		    return new ServiceMethodInstanceData<AdminRoutes>
		    {
			    instance = instance,
			    provider = args.ServiceScope.Fork()
		    };
	    });
    }

    public BeamServiceConfigBuilder ExcludeRoutes<T>()
    {
	    var conf = Config as IBeamServiceConfig;
	    conf.RouteSource.RemoveAll(s => s.InstanceType == typeof(T));
	    return this;
    }
    
    public BeamServiceConfigBuilder IncludeRoutes<T>(
	    string routePrefix=null, 
	    string clientPrefix=null,
	    BeamRouteTypeActivator<T> activator=null)
	    where T : class
    {
	    var conf = Config as IBeamServiceConfig;
	    
	    // note: an empty string should be a valid alternative, because that is the backwards compat layer. 
	    routePrefix ??= typeof(T).Name;
	    clientPrefix ??= typeof(T).Name;
	    
	    var route = new BeamRouteSource
	    {
		    RoutePrefix = routePrefix,
		    ClientNamespacePrefix = clientPrefix,
		    InstanceType = typeof(T),
	    };
	    if (activator == null)
	    {
		    route.Activator = (args, context) => RouteSourceUtil.ActivateInstance(route.InstanceType, args, context);
	    }
	    else
	    {
		    route.Activator = (args, reqCtx) =>
		    {
			    var typed = activator(args, reqCtx);
			    return new ServiceMethodInstanceData
			    {
				    provider = typed.provider,
				    instance = typed.instance
			    };
		    };
	    }

	    conf.RouteSource.Add(route);
	    return this;
    }

    public BeamServiceConfigBuilder ConfigureServices(Action<IDependencyBuilder> builder)
    {
	    var conf = Config as IBeamServiceConfig;
	    conf.ServiceConfigurations.Add(builder);
	    return this;
    }

    public BeamServiceConfigBuilder InitializeServices(Func<Task> initializer)
    {
	    return InitializeServices(async _ =>
	    {
		    await initializer();
	    });
    }
    
    public BeamServiceConfigBuilder InitializeServices(Func<IDependencyProviderScope, Task> initializer)
    {
	    var conf = Config as IBeamServiceConfig;
	    conf.ServiceInitializers.Add(initializer);
	    return this;
    }

    public BeamServiceConfigBuilder InitializeServices(Action initializer)
    {
	    return InitializeServices(_ => initializer());
    }
    
    public BeamServiceConfigBuilder InitializeServices(Action<IDependencyProviderScope> initializer)
    {
	    return InitializeServices(scope =>
	    {
		    initializer(scope);
		    return Task.CompletedTask;
	    });
    }
    
    public BeamServiceConfigBuilder Override(Action<IBeamServiceConfig> configurator)
    {
        configurator?.Invoke(Config as IBeamServiceConfig);
        return this;
    }
    
    public async Task<MicroserviceResult> Start()
    {
	    return await MicroserviceBootstrapper.Begin(Config);
    }
    
    public async Task RunForever()
    {
	    await MicroserviceBootstrapper
		    .Begin(Config)
		    .RunForever();
    }
}

public static class MicroserviceResultExtensions
{
	public static async Task RunForever(this Task<MicroserviceResult> res)
	{
		var result = await res;
		if (result.GeneratedClient) return;
		
		await Task.Delay(-1);
	}
}

public static class IBeamServiceConfigExtensions
{
    // TODO: look they can be extension methods, so people can tack on.
    public static BeamServiceConfigBuilder DisableEagerContentLoading(this BeamServiceConfigBuilder builder)
    {
	    var conf = builder.Config as IBeamServiceConfig;
	    conf.Attributes.EnableEagerContentLoading = false;
	    return builder;
    }
}