using Beamable.Server;
using microservice.Common;

namespace beamable.tooling.common.Microservice;

public static class RouteTableGeneration
{
	public static ServiceMethodCollection BuildRoutes(Type microserviceType, MicroserviceAttribute serviceAttribute, AdminRoutes adminRoutes, Func<RequestContext, object> serviceFactory)
	{
		var clientGenerator = new ServiceMethodProvider
		{
			instanceType = microserviceType, factory = serviceFactory, pathPrefix = ""
		};
		var generators = adminRoutes == null
			? new ServiceMethodProvider[] { clientGenerator }
			: new ServiceMethodProvider[]
			{
				new ServiceMethodProvider
				{
					instanceType = typeof(AdminRoutes), factory = _ => adminRoutes, pathPrefix = "admin/"
				},
				clientGenerator
			};
		
		var collection = ServiceMethodHelper.Scan(serviceAttribute,
			new ICallableGenerator[]
			{
				new FederatedLoginCallableGenerator(),
				new FederatedInventoryCallbackGenerator()
			},
			generators
			);
		return collection;
	}
}
