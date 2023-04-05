using Beamable.Server;
using microservice.Common;

namespace beamable.tooling.common.Microservice;

public static class RouteTableGeneration
{
	public static ServiceMethodCollection BuildRoutes(Type microserviceType, MicroserviceAttribute serviceAttribute, AdminRoutes adminRoutes, Func<RequestContext, object> serviceFactory)
	{
		var collection = ServiceMethodHelper.Scan(serviceAttribute,
			new ICallableGenerator[]
			{
				new FederatedLoginCallableGenerator(),
				new FederatedInventoryCallbackGenerator()
			},
			new ServiceMethodProvider
			{
				instanceType = typeof(AdminRoutes), factory = _ => adminRoutes, pathPrefix = "admin/"
			},
			new ServiceMethodProvider
			{
				instanceType = microserviceType, factory = serviceFactory, pathPrefix = ""
			});
		return collection;
	}
}
