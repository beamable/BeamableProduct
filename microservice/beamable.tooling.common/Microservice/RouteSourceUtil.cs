using System.Collections.Generic;
using Beamable.Common.Dependencies;
using Beamable.Server;
using microservice.Common;
using Microsoft.Extensions.DependencyInjection;

namespace beamable.tooling.common.Microservice;

/// <summary>
/// Provides utility methods for generating route tables.
/// </summary>
public static class RouteSourceUtil
{
	public static ServiceMethodInstanceData ActivateInstance(Type type, IMicroserviceArgs args, MicroserviceRequestContext context)
	{
		IDependencyProviderScope instanceScope = null;
		var isZone = args.ServiceScope.GetService<IMicroserviceAttributes>()?.GetServiceScope() ==
		             BeamServiceScope.Zone;
		instanceScope = args.ServiceScope.Fork(builder =>
		{
			// each _request_ gets its own service scope, so we fork the provider again and override certain services.
			builder.AddScoped(context);
			builder.AddScoped<RequestContext>(context);
			builder.AddScoped(args);

			builder.AddScoped<IUserScope>(p => new UserRequestDataHandler((IDependencyProviderScope)p));

			if (isZone)
			{
				// Zone services have no realm/player; hand them a zone request context (cid + zid) instead of
				// the realm one. TODO(zones): build this lazily from the JsonElement like
				// MicroserviceRequestContext, rather than eagerly reading Body/Headers here.
				var zoned = new ZonedRequestContext(args.CustomerID, args.Zid, context.Id, context.Status,
					context.Path, context.Method, context.Body, new HashSet<string>(context.Scopes),
					context.Headers);
				builder.AddScoped(zoned);
				builder.AddScoped<IRequestContext>(zoned);
			}
			else
			{
				builder.AddScoped<IRequestContext>(context);
			}
		});
		
		// construct the instance from the new scope,
		//  any DI arguments the service requires will
		//  get them from this newScope
		var service = instanceScope.GetRequiredService(type);
		if (service is IUserScopeCallbackReceiver receiver)
		{
			receiver.ReceiveDefaultServices(instanceScope);
		}
		return new ServiceMethodInstanceData
		{
			provider = instanceScope,
			instance = service
		};
	}
	
	public static ServiceMethodCollection BuildRoutes(StartupContext startupContext, IMicroserviceArgs instanceArgs)
	{
		var providers = new ServiceMethodProvider[startupContext.routeSources.Length];
		for (var i = 0; i < startupContext.routeSources.Length; i++)
		{
			var routeType = startupContext.routeSources[i];
			providers[i] = new ServiceMethodProvider
			{
				instanceType = routeType.InstanceType,
				pathPrefix = routeType.RoutePrefix,
				clientPrefix = routeType.ClientNamespacePrefix,
				factory = requestContext => routeType.Activator(instanceArgs, requestContext)
			};
		}
		
		var collection = ServiceMethodHelper.Scan(startupContext.attributes, providers);
		return collection;
	}
}
