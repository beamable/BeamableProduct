using Beamable.Server;
using microservice.Common;

namespace beamable.tooling.common.Microservice;

/// <summary>
/// Provides utility methods for generating route tables.
/// </summary>
public static class RouteTableGeneration
{
	/// <summary>
	/// Builds a collection of service methods and associated routes.
	/// </summary>
	/// <param name="microserviceType">The type of the microservice for which to build routes.</param>
	/// <param name="serviceAttribute">The MicroserviceAttribute associated with the microservice.</param>
	/// <param name="adminRoutes">The administrative routes associated with the microservice.</param>
	/// <param name="serviceFactory">A function to create instances of the microservice.</param>
	/// <returns>A collection of service methods and associated routes.</returns>
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
				new FederatedInventoryCallbackGenerator(),
				new FederatedGameServerCallableGenerator(),
				new FederatedPlayerInitCallableGenerator()
			},
			generators
			);
		return collection;
	}
}
