using Beamable.Common.Dependencies;
using System;
using Beamable.Server;

namespace microservice
{
    public class DefaultServiceInitializer : IServiceInitializer
    {
        private readonly IDependencyProviderScope _provider;

        public IDependencyProvider Provider => _provider;
        
        public DefaultServiceInitializer(IDependencyProviderScope provider, IMicroserviceArgs microserviceArgs)
        {
            _provider = provider;
        }
        
        public TService GetServiceAsCache<TService>() 
            where TService : class
        {
	        if (!_provider.TryGetServiceLifetime<TService>(out var lifetime))
	        {
		        throw new MissingServiceException($"No registered service implementation for service interface [{nameof(TService)}]!\n" +
		                                          $"Please use {nameof(ConfigureServicesAttribute)} to register an implementation for this service.");
	        }
          
            if (lifetime != DependencyLifetime.Singleton)
                throw new InaccessibleServiceException($"You can only access singleton services during service initialization.\n" +
                                                       $"Other services are instantiated per-request, so they're data would not be kept alive by the Dependency Injection framework.\n" +
                                                       $"Please register the implementation for the service interface [{typeof(TService).Name}] via the [{nameof(IServiceBuilder.AddSingleton)}] method" +
                                                       $"inside a [{nameof(ConfigureServicesAttribute)}] method.");
            
            return (TService) _provider.GetService(typeof(TService));
        }
        
        public TService GetService<TService>() 
            where TService : class
        {
	        return (TService) _provider.GetService(typeof(TService));
        }

        public object GetService(Type serviceType)
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
