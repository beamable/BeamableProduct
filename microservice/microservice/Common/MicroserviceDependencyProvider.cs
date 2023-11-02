using System;
using Beamable.Common.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Beamable.Server
{
    public class MicrosoftServiceProviderWrapper : IDependencyProvider
    {
        private readonly IServiceProvider _provider;

        public MicrosoftServiceProviderWrapper(IServiceProvider provider)
        {
            _provider = provider;
        }

        public bool CanBuildService(Type t) => _provider.GetService(t) != null;

        public object GetService(Type t) => _provider.GetRequiredService(t);

        public T GetService<T>() => _provider.GetRequiredService<T>();

        public bool CanBuildService<T>() => _provider.GetService<T>() != null;

        public IDependencyProviderScope Fork(Action<IDependencyBuilder> configure = null)
        {
            throw new NotImplementedException("This isn't available in a C#MS context, sorry.");
        }

        public IDependencyProviderScope Fork(IDependencyBuilder configuration)
        {
	        throw new NotImplementedException("This isn't available in a C#MS context, sorry.");
        }
    }
}
