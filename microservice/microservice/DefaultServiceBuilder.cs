using Beamable.Server;
using Microsoft.Extensions.DependencyInjection;

namespace microservice
{
	public class DefaultServiceBuilder : IServiceBuilder
	{
		private ServiceCollection Collection { get; }

		public DefaultServiceBuilder(ServiceCollection collection)
		{
			Collection = collection ?? new ServiceCollection();
		}

		public void AddTransient<T>(ServiceFactory<T> factory) =>
		   Collection.AddTransient(typeof(T), provider => factory(provider));

		public void AddSingleton<T>(ServiceFactory<T> factory) =>
		   Collection.AddSingleton(typeof(T), provider => factory(provider));

		public void AddScoped<T>(ServiceFactory<T> factory) =>
		   Collection.AddScoped(typeof(T), provider => factory(provider));

		public void AddTransient<T>() => Collection.AddTransient(typeof(T));

		public void AddSingleton<T>() => Collection.AddSingleton(typeof(T));

		public void AddScoped<T>() => Collection.AddScoped(typeof(T));

		public void AddTransient<TService, TImplementation>()
		   where TService : class
		   where TImplementation : class, TService => Collection.AddTransient<TService, TImplementation>();

		public void AddSingleton<TService, TImplementation>()
		   where TService : class
		   where TImplementation : class, TService => Collection.AddSingleton<TService, TImplementation>();

		public void AddScoped<TService, TImplementation>()
		   where TService : class
		   where TImplementation : class, TService => Collection.AddScoped<TService, TImplementation>();

	}
}
