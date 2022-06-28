using Beamable.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace microservice
{
	public class DefaultServiceInitializer : IServiceInitializer
	{
		private readonly IServiceCollection _collection;
		private readonly IServiceProvider _provider;

		public DefaultServiceInitializer(IServiceCollection serviceCollection, IMicroserviceArgs microserviceArgs)
		{
			_collection = serviceCollection;
			serviceCollection.AddScoped(_ => new RequestContext(microserviceArgs.CustomerID, microserviceArgs.ProjectName));
			_provider = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;
		}

		public TService GetServiceAsCache<TService>()
			where TService : class
		{
			var foundService = _collection.First(descriptor => descriptor.ServiceType == typeof(TService));
			if (foundService == null)
				throw new MissingServiceException($"No registered service implementation for service interface [{nameof(TService)}]!\n" +
												  $"Please use {nameof(ConfigureServicesAttribute)} to register an implementation for this service.");

			if (foundService.Lifetime != ServiceLifetime.Singleton)
				throw new InaccessibleServiceException($"You can only access singleton services during service initialization.\n" +
													   $"Other services are instantiated per-request, so they're data would not be kept alive by the Dependency Injection framework.\n" +
													   $"Please register the implementation for the service interface [{typeof(TService).Name}] via the [{nameof(IServiceBuilder.AddSingleton)}] method" +
													   $"inside a [{nameof(ConfigureServicesAttribute)}] method.");


			return (TService)_provider.GetService(typeof(TService));
		}

		public TService GetService<TService>()
			where TService : class
		{
			var foundService = _collection.First(descriptor => descriptor.ServiceType == typeof(TService));
			if (foundService == null)
				throw new MissingServiceException($"No registered service implementation for service interface [{nameof(TService)}]!\n" +
												  $"Please use {nameof(ConfigureServicesAttribute)} to register an implementation for this service.");

			return (TService)_provider.GetService(typeof(TService));
		}

		public object? GetService(Type serviceType)
		{
			return _provider.GetService(serviceType);
		}
	}


	public class MissingServiceException : System.Exception
	{
		public MissingServiceException(string message = null) : base(message)
		{
		}
	}

	public class InaccessibleServiceException : System.Exception
	{
		public InaccessibleServiceException(string message = null) : base(message)
		{
		}
	}
}
