using System;

namespace Beamable.Server
{
   public delegate T ServiceFactory<out T>(IServiceProvider provider);

   public interface IServiceBuilder
   {
      void AddTransient<T>(ServiceFactory<T> factory);
      void AddSingleton<T>(ServiceFactory<T> factory);
      void AddScoped<T>(ServiceFactory<T> factory);

      void AddTransient<T>();
      void AddSingleton<T>();
      void AddScoped<T>();

      void AddTransient<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService;
      void AddSingleton<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService;
      void AddScoped<TService, TImplementation>()
         where TService : class
         where TImplementation : class, TService;
   }
}