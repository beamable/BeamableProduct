
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.Dependencies
{
	public interface IDependencyProvider
	{
		T GetService<T>();
		object GetService(Type t);
	}

	public interface IDependencyProviderScope : IDependencyProvider
	{
		Promise Dispose();
	}

	public interface IBeamableDisposable
	{
		Promise OnDispose();
	}

	public class DependencyProvider : IDependencyProviderScope
	{
		public Dictionary<Type, ServiceDescriptor> Transients { get; set; }

		public Dictionary<Type, ServiceDescriptor> Singletons { get; set; }
		private Dictionary<Type, object> SingletonCache { get; set; } = new Dictionary<Type, object>();

		private bool _destroyed;

		private List<object> _createdTransientServices = new List<object>();

		public T GetService<T>()
		{
			return (T)GetService(typeof(T));
		}

		public object GetService(Type t)
		{
			if (_destroyed) throw new Exception("Provider scope has been destroyed and can no longer be accessed.");

			if (Transients.TryGetValue(t, out var descriptor))
			{
				var service = descriptor.Factory(this);
				_createdTransientServices.Add(service);
				return service;
			}

			if (Singletons.TryGetValue(t, out descriptor))
			{
				if (SingletonCache.TryGetValue(t, out var instance))
				{
					return instance;
				}

				return SingletonCache[t] = descriptor.Factory(this);
			}

			throw new Exception($"Service not found {t.Name}");
		}

		public async Promise Dispose()
		{
			_destroyed = true;
			var disposalPromises = new List<Promise<Unit>>();
			var gameObjectsForDeletion = new HashSet<GameObject>();

			void DisposeServices(IEnumerable<object> services)
			{
				foreach (var service in services)
				{

					if (service == null) continue;
					if (service is IBeamableDisposable disposable)
					{
						var promise = disposable.OnDispose();
						if (promise != null)
						{
							disposalPromises.Add(promise);
						}

						if (disposable is Component disposableComponent)
						{
							gameObjectsForDeletion.Add(disposableComponent.gameObject);
						}
					}
					else if (service is Component component)
					{
						// automatic behaviour for components that don't have the IBeamableDisposable interface is to destroy them
						UnityEngine.Object.Destroy(component);
						gameObjectsForDeletion.Add(component.gameObject);
					}
				}
			}

			DisposeServices(_createdTransientServices);
			DisposeServices(SingletonCache.Values);

			foreach (var gob in gameObjectsForDeletion)
			{
				if (gob.GetComponents<Component>().Length <= 1) // allow for a transform component
				{
					UnityEngine.Object.Destroy(gob);
				}
			}
			await Promise.Sequence(disposalPromises);

			_createdTransientServices.Clear();
			SingletonCache.Clear();
		}
	}

	public interface IDependencyBuilder
	{
		IDependencyBuilder AddTransient<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddTransient<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddTransient<TInterface, TImpl>() where TImpl : TInterface;
		IDependencyBuilder AddTransient<T>(Func<IDependencyProvider, T> factory);
		IDependencyBuilder AddTransient<T>(Func<T> factory);
		IDependencyBuilder AddTransient<T>();

		IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>(TInterface service) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>() where TImpl : TInterface;
		IDependencyBuilder AddSingleton<T>(Func<IDependencyProvider, T> factory);
		IDependencyBuilder AddSingleton<T>(Func<T> factory);
		IDependencyBuilder AddSingleton<T>(T service);
		IDependencyBuilder AddSingleton<T>();

		IDependencyProviderScope Build();

		IDependencyBuilder Fork();
	}

	public class ServiceDescriptor
	{
		public Type Interface, Implementation;
		public Func<IDependencyProvider, object> Factory;
	}

	public class DependencyBuilder : IDependencyBuilder
	{
		private List<ServiceDescriptor> _transientServices = new List<ServiceDescriptor>();
		private List<ServiceDescriptor> _singletonServices = new List<ServiceDescriptor>();

		public IDependencyBuilder AddTransient<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface
		{
			_transientServices.Add(new ServiceDescriptor {
				Interface = typeof(TInterface),
				Implementation = typeof(TImpl),
				Factory = (provider) => factory(provider)
			});
			return this;
		}

		public IDependencyBuilder AddTransient<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface =>
			AddTransient<TInterface, TImpl>(_ => factory());

		public IDependencyBuilder AddTransient<TInterface, TImpl>() where TImpl : TInterface =>
			AddTransient<TInterface, TImpl>(factory => Instantiate<TImpl>(factory));

		public IDependencyBuilder AddTransient<T>(Func<IDependencyProvider, T> factory) => AddTransient<T, T>(factory);

		public IDependencyBuilder AddTransient<T>(Func<T> factory) => AddTransient<T, T>(factory);

		public IDependencyBuilder AddTransient<T>() => AddTransient<T, T>();

		public IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface
		{
			_singletonServices.Add(new ServiceDescriptor {
				Interface = typeof(TInterface),
				Implementation = typeof(TImpl),
				Factory = (provider) => factory(provider)
			});
			return this;
		}

		public IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface =>
			AddSingleton<TInterface, TImpl>(_ => factory());

		public IDependencyBuilder AddSingleton<TInterface, TImpl>(TInterface service) where TImpl : TInterface =>
			AddSingleton<TInterface, TImpl>(_ => service);

		public IDependencyBuilder AddSingleton<TInterface, TImpl>() where TImpl : TInterface =>
			AddSingleton<TInterface, TImpl>(factory => Instantiate<TImpl>(factory));

		public IDependencyBuilder AddSingleton<T>(Func<IDependencyProvider, T> factory) => AddSingleton<T, T>(factory);

		public IDependencyBuilder AddSingleton<T>(Func<T> factory) => AddSingleton<T, T>(factory);

		public IDependencyBuilder AddSingleton<T>(T service) => AddSingleton<T, T>(service);

		public IDependencyBuilder AddSingleton<T>() => AddSingleton<T, T>();

		private TImpl Instantiate<TImpl>(IDependencyProvider provider)
		{
			// TODO: XXX: This only works for the first constructor; really it should scan for the first constructor it can match
			var cons = typeof(TImpl).GetConstructors().FirstOrDefault();
			var parameters = cons.GetParameters();
			var values = new object[parameters.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				values[i] = provider.GetService(parameters[i].ParameterType);
			}

			var instance = (TImpl)cons?.Invoke(values);
			return instance;
		}

		public IDependencyProviderScope Build()
		{
			return new DependencyProvider {
				Transients = _transientServices.ToDictionary(s => s.Interface),
				Singletons = _singletonServices.ToDictionary(s => s.Interface)
			};
		}

		public IDependencyBuilder Fork()
		{
			return new DependencyBuilder {
				_singletonServices = _singletonServices.ToList(),
				_transientServices = _transientServices.ToList()
			};
		}
	}

}
