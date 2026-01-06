using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using Beamable.Server.Content;
using beamable.tooling.common.Microservice;
using microservice.Common;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Beamable.Server;



public static partial class MicroserviceBootstrapper
{
}

public class BeamServiceConfig : IBeamServiceConfig
{
	IMicroserviceArgs IBeamServiceConfig.Args { get; set; } = new EnvironmentArgs();
    IMicroserviceAttributes IBeamServiceConfig.Attributes { get; set; }
    List<BeamRouteSource> IBeamServiceConfig.RouteSources { get; set; } = new List<BeamRouteSource>();
    
    string IBeamServiceConfig.LocalEnvCustomArgs { get; set; }
    List<Action<IDependencyBuilder>> IBeamServiceConfig.ServiceConfigurations { get; set; } = new List<Action<IDependencyBuilder>>();

    List<Func<IDependencyProviderScope, Task>> IBeamServiceConfig.ServiceInitializers { get; set; } = new List<Func<IDependencyProviderScope, Task>>();
    Action<ILoggingBuilder> IBeamServiceConfig.AddLoggerProvider { get; set; }
    Action<IBeamableService> IBeamServiceConfig.FirstConnectionHandler { get; set; }
}

public static class BeamServiceConfigExtensions
{
	public static IBeamServiceConfig ExcludeRoutes<T>(this IBeamServiceConfig conf)
	{
		conf.RouteSources.RemoveAll(s => s.InstanceType == typeof(T));
		return conf;
	}
	
	public static IBeamServiceConfig IncludeRoutes<T>(this IBeamServiceConfig conf, 
		string routePrefix=null, 
		string clientPrefix=null,
		BeamRouteTypeActivator<T> activator=null)
	where T: class
	{
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

		conf.RouteSources.Add(route);
		return conf;
	}

	public static IBeamServiceConfig WithAdminRoutes(this IBeamServiceConfig conf)
	{
		AdminRoutes instance = null;
		return conf.IncludeRoutes<AdminRoutes>("admin/", null, (args, reqCtx) =>
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
				provider = args.ServiceScope.Fork() // TODO: I don't think we need the provider AT ALL in the admin routes
			};
		});
	}

	public static IBeamServiceConfig ConfigureServices(this IBeamServiceConfig conf, Action<IDependencyBuilder> configurator)
	{
		conf.ServiceConfigurations.Add(configurator);
		return conf;
	}

	public static IBeamServiceConfig InitializeServices(this IBeamServiceConfig conf,
		Func<IDependencyProviderScope, Task> initializer)
	{
		conf.ServiceInitializers.Add(initializer);
		return conf;
	}
	
}

public interface IBeamServiceConfig
{
    IMicroserviceArgs Args { get; set; }
    string LocalEnvCustomArgs { get; set; }
    IMicroserviceAttributes Attributes { get; set; }
    List<BeamRouteSource> RouteSources { get; set; }
    List<Action<IDependencyBuilder>> ServiceConfigurations { get; set; }
    List<Func<IDependencyProviderScope, Task>> ServiceInitializers { get; set; }
    Action<ILoggingBuilder> AddLoggerProvider { get; set; }
    Action<IBeamableService> FirstConnectionHandler { get; set; }
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

	    conf.Attributes = attributes ?? BuiltSettings.ReadServiceAttributes(conf.Args) ?? new DefaultMicroserviceAttributes();
	    conf.WithAdminRoutes();
    }

    public BeamServiceConfigBuilder ExcludeRoutes<T>()
    {
	    Config.ExcludeRoutes<T>();
	    return this;
    }
    
    public BeamServiceConfigBuilder IncludeRoutes<T>(
	    string routePrefix=null, 
	    string clientPrefix=null,
	    BeamRouteTypeActivator<T> activator=null)
	    where T : class
    {
	    Config.IncludeRoutes(routePrefix, clientPrefix, activator);
	    return this;
    }

    public BeamServiceConfigBuilder ConfigureServices(Action<IDependencyBuilder> configurator)
    {
	    Config.ConfigureServices(configurator);
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
	    Config.InitializeServices(initializer);
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
    
    public BeamServiceConfigBuilder OverrideConfig(Action<IBeamServiceConfig> configurator)
    {
        configurator?.Invoke(Config);
        return this;
    }
    
    /// <summary>
    /// Note: Any code you run after this method will run on every autoscaled instance of the microservice
    /// running in the Beamable cloud.
    /// </summary>
    /// <returns></returns>
    public async Task<MicroserviceResult> Run()
    {
	    return await MicroserviceStartupUtil.Begin(Config);
    }
    
    public async Task RunForever()
    {
	    await MicroserviceStartupUtil
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
	public static BeamServiceConfigBuilder UseRealmSecretAuth(this BeamServiceConfigBuilder builder)
	{
		var conf = builder.Config as IBeamServiceConfig;
		conf.LocalEnvCustomArgs = " . --auto-deploy --include-prefix ";
		return builder;
	}
	
	public static BeamServiceConfigBuilder AddTelemetryAttributes<T>(this BeamServiceConfigBuilder builder)
		where T : ITelemetryAttributeProvider
	{
		builder.ConfigureServices(b =>
		{
			b.AddSingleton<T>();
		});
		return builder;
	}
	
    public static BeamServiceConfigBuilder DisableEagerContentLoading(this BeamServiceConfigBuilder builder)
    {
	    var conf = builder.Config as IBeamServiceConfig;
	    conf.Attributes.EnableEagerContentLoading = false;
	    return builder;
    }

    public static BeamServiceConfigBuilder DisableAllBeamableEvents(this BeamServiceConfigBuilder builder)
    {
	    var conf = builder.Config as IBeamServiceConfig;
	    conf.Attributes.DisableAllBeamableEvents = true;
	    return builder;
    }
    
    public static BeamServiceConfigBuilder WithCustomAutoGeneratedClientPath(this BeamServiceConfigBuilder builder, string path)
    {
	    var conf = builder.Config as IBeamServiceConfig;
	    conf.Attributes.CustomAutoGeneratedClientPath = path;
	    return builder;
    }
}
