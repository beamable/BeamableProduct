using Beamable.Common.Dependencies;
using Beamable.Server;

namespace microservice
{
   public class DefaultServiceBuilder : IServiceBuilder
   {
      public IDependencyBuilder Builder { get; }

      public DefaultServiceBuilder(IDependencyBuilder builder)
      {
         Builder = builder ?? new DependencyBuilder();
      }

      public void AddTransient<T>(ServiceFactory<T> factory) => Builder.AddTransient(provider => factory(provider));

      public void AddSingleton<T>(ServiceFactory<T> factory) => Builder.AddSingleton(provider => factory(provider));

      public void AddScoped<T>(ServiceFactory<T> factory) => Builder.AddScoped(provider => factory(provider));

      public void AddTransient<T>() => Builder.AddTransient<T>();

      public void AddSingleton<T>() => Builder.AddSingleton<T>();

      public void AddScoped<T>() => Builder.AddScoped<T>();

      public void AddTransient<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService => Builder.AddTransient<TService, TImplementation>();

      public void AddSingleton<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService => Builder.AddSingleton<TService, TImplementation>();

      public void AddScoped<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService => Builder.AddScoped<TService, TImplementation>();

   }
}
