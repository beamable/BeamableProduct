
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.Dependencies
{
	public interface IDependencyProvider
	{
		bool CanBuildService(Type t);
		object GetService(Type t);
		T GetService<T>();
		bool CanBuildService<T>();
		IDependencyProviderScope Fork(Action<IDependencyBuilder> configure = null);
	}

	public interface IDependencyProviderScope : IDependencyProvider
	{
		Promise Dispose();
		IDependencyProviderScope Parent { get; }
		IEnumerable<IDependencyProviderScope> Children { get; }
		void Hydrate(IDependencyProviderScope serviceScope);

		bool IsDestroyed { get; }
		IEnumerable<ServiceDescriptor> TransientServices { get; }
		IEnumerable<ServiceDescriptor> ScopedServices{ get; }
		IEnumerable<ServiceDescriptor> SingletonServices{ get; }
	}

	public interface IBeamableDisposable
	{
		Promise OnDispose();
	}

	public class DependencyProvider : IDependencyProviderScope
	{
		public Dictionary<Type, ServiceDescriptor> Transients { get; set; }

		public Dictionary<Type, ServiceDescriptor> Scoped { get; set; }
		public Dictionary<Type, ServiceDescriptor> Singletons { get; set; }

		private Dictionary<Type, object> SingletonCache { get; set; } = new Dictionary<Type, object>();
		private Dictionary<Type, object> ScopeCache { get; set; } = new Dictionary<Type, object>();

		private bool _destroyed;

		private List<object> _createdTransientServices = new List<object>();


		public bool IsDestroyed => _destroyed;
		public IEnumerable<ServiceDescriptor> TransientServices => Transients.Values;
		public IEnumerable<ServiceDescriptor> ScopedServices => Scoped.Values;
		public IEnumerable<ServiceDescriptor> SingletonServices => Singletons.Values;

		public IDependencyProviderScope Parent { get; protected set; }
		private HashSet<IDependencyProviderScope> _children = new HashSet<IDependencyProviderScope>();

		private Dictionary<IDependencyProviderScope, Action<IDependencyBuilder>> _childToConfigurator =
			new Dictionary<IDependencyProviderScope, Action<IDependencyBuilder>>();
		public IEnumerable<IDependencyProviderScope> Children => _children;

		public Guid Id = Guid.NewGuid();

		public DependencyProvider(DependencyBuilder builder)
		{
			Transients = builder.TransientServices.ToDictionary(s => s.Interface);
			Scoped = builder.ScopedServices.ToDictionary(s => s.Interface);
			Singletons = builder.SingletonServices.ToDictionary(s => s.Interface);
		}

		public T GetService<T>()
		{
			return (T)GetService(typeof(T));
		}

		public bool CanBuildService<T>()
		{
			return CanBuildService(typeof(T));
		}

		public bool CanBuildService(Type t)
		{
			if (_destroyed) throw new Exception("Provider scope has been destroyed and can no longer be accessed.");

			return Transients.ContainsKey(t) || Scoped.ContainsKey(t) || Singletons.ContainsKey(t) || (Parent?.CanBuildService(t) ?? false);
		}

		public object GetService(Type t)
		{
			if (_destroyed) throw new Exception("Provider scope has been destroyed and can no longer be accessed.");

			if (t == typeof(IDependencyProvider)) return this;

			if (Transients.TryGetValue(t, out var descriptor))
			{
				var service = descriptor.Factory(this);
				_createdTransientServices.Add(service);
				return service;
			}

			if (Scoped.TryGetValue(t, out descriptor))
			{
				if (ScopeCache.TryGetValue(t, out var instance))
				{
					return instance;
				}

				return ScopeCache[t] = descriptor.Factory(this);
			}


			if (Singletons.TryGetValue(t, out descriptor))
			{
				if (SingletonCache.TryGetValue(t, out var instance))
				{
					return instance;
				}

				return SingletonCache[t] = descriptor.Factory(this);
			}

			if (Parent != null)
			{
				return Parent.GetService(t);
			}


			throw new Exception($"Service not found {t.Name}");
		}

		public async Promise Dispose()
		{
			if (_destroyed) return; // don't dispose twice!

			_destroyed = true;
			var disposalPromises = new List<Promise<Unit>>();
			var gameObjectsForDeletion = new HashSet<GameObject>();

			// remove from parent.

			foreach (var child in _children)
			{
				child?.Dispose();
			}

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
			DisposeServices(ScopeCache.Values);

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
			ScopeCache.Clear();
		}

		public void Hydrate(IDependencyProviderScope other)
		{
			_destroyed = other.IsDestroyed;
			Transients = other.TransientServices.ToDictionary(desc => desc.Interface);
			Scoped = other.ScopedServices.ToDictionary(desc => desc.Interface);
			Singletons = other.SingletonServices.ToDictionary(desc => desc.Interface);
			SingletonCache.Clear();
			ScopeCache.Clear();

			var oldChildren = new HashSet<IDependencyProviderScope>(_children);
			var newChildren = new HashSet<IDependencyProviderScope>();
			foreach (var child in oldChildren)
			{
				var configurator = _childToConfigurator[child];
				var newChild = Fork(configurator);
				newChildren.Add(newChild);
				child.Hydrate(newChild);
			}

			foreach (var child in newChildren)
			{
				_children.Remove(child);
			}
		}


		public IDependencyProviderScope Fork(Action<IDependencyBuilder> configure=null)
		{
			var builder = new DependencyBuilder();
			// populate all of the existing services we have in this scope.

			void AddDescriptors(List<ServiceDescriptor> target, Dictionary<Type, ServiceDescriptor> source, Func<IDependencyProvider, ServiceDescriptor, object> factory)
			{
				foreach (var kvp in source)
				{
					target.Add(new ServiceDescriptor {
						Implementation = kvp.Value.Implementation,
						Interface = kvp.Value.Interface,
						Factory = p => factory(p, kvp.Value)
					});
				}
			}

			// transients are stupid, and I should probably delete them.
			AddDescriptors(builder.TransientServices, Transients,(nextProvider, desc) => desc.Factory(nextProvider));


			// all scoped descriptors
			AddDescriptors(builder.ScopedServices, Scoped,(nextProvider, desc) => desc.Factory(nextProvider));
			// scopes services build brand new instances per provider


			// singletons use their parent singleton cache.
			AddDescriptors(builder.SingletonServices, Scoped, (_, desc) => GetService(desc.Interface));

			configure?.Invoke(builder);

			var provider = new DependencyProvider(builder) {Parent = this};
			_children.Add(provider);
			_childToConfigurator[provider] = configure;

			return provider;
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

		IDependencyBuilder AddScoped<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddScoped<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddScoped<TInterface, TImpl>(TInterface service) where TImpl : TInterface;
		IDependencyBuilder AddScoped<TInterface, TImpl>() where TImpl : TInterface;
		IDependencyBuilder AddScoped<T>(Func<IDependencyProvider, T> factory);
		IDependencyBuilder AddScoped<T>(Func<T> factory);
		IDependencyBuilder AddScoped<T>(T service);
		IDependencyBuilder AddScoped<T>();

		IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>(TInterface service) where TImpl : TInterface;
		IDependencyBuilder AddSingleton<TInterface, TImpl>() where TImpl : TInterface;
		IDependencyBuilder AddSingleton<T>(Func<IDependencyProvider, T> factory);
		IDependencyBuilder AddSingleton<T>(Func<T> factory);
		IDependencyBuilder AddSingleton<T>(T service);
		IDependencyBuilder AddSingleton(Type t);
		IDependencyBuilder AddSingleton<T>();

		IDependencyProviderScope Build();

		IDependencyBuilder Remove<T>();
		IDependencyBuilder RemoveIfExists<T>();
		bool Has<T>();
		IDependencyBuilder Fork();
	}

	public class ServiceDescriptor
	{
		public Type Interface, Implementation;
		public Func<IDependencyProvider, object> Factory;
	}

	public class DependencyBuilder : IDependencyBuilder
	{
		public List<ServiceDescriptor> TransientServices { get; protected set; } = new List<ServiceDescriptor>();
		public List<ServiceDescriptor> ScopedServices { get; protected set; } = new List<ServiceDescriptor>();
		public List<ServiceDescriptor> SingletonServices { get; protected set; } = new List<ServiceDescriptor>();


		public IDependencyBuilder AddTransient<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface
		{
			TransientServices.Add(new ServiceDescriptor {
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

		public IDependencyBuilder AddScoped<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface
		{
			ScopedServices.Add(new ServiceDescriptor {
				Interface = typeof(TInterface),
				Implementation = typeof(TImpl),
				Factory = (provider) => factory(provider)
			});
			return this;
		}

		public IDependencyBuilder AddScoped<TInterface, TImpl>(Func<TInterface> factory) where TImpl : TInterface =>
			AddScoped<TInterface, TImpl>(_ => factory());

		public IDependencyBuilder AddScoped<TInterface, TImpl>(TInterface service) where TImpl : TInterface =>
			AddScoped<TInterface, TImpl>(_ => service);

		public IDependencyBuilder AddScoped<TInterface, TImpl>() where TImpl : TInterface =>
			AddScoped<TInterface, TImpl>(factory => Instantiate<TImpl>(factory));

		public IDependencyBuilder AddScoped<T>(Func<IDependencyProvider, T> factory) => AddScoped<T, T>(factory);

		public IDependencyBuilder AddScoped<T>(Func<T> factory) => AddScoped<T, T>(factory);

		public IDependencyBuilder AddScoped<T>(T service) => AddScoped<T, T>(service);

		public IDependencyBuilder AddScoped<T>() => AddScoped<T, T>();


		//

		public IDependencyBuilder AddSingleton<TInterface, TImpl>(Func<IDependencyProvider, TInterface> factory) where TImpl : TInterface
		{
			SingletonServices.Add(new ServiceDescriptor {
				Interface = typeof(TInterface),
				Implementation = typeof(TImpl),
				Factory = (provider) => factory(provider)
			});
			return this;
		}

		public IDependencyBuilder AddSingleton(Type type)
		{
			SingletonServices.Add(new ServiceDescriptor {
				Interface = type,
				Implementation = type,
				Factory = provider => Instantiate(type, provider)
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
			return (TImpl)Instantiate(typeof(TImpl), provider);
		}

		private object Instantiate(Type type, IDependencyProvider provider)
		{
			// TODO: XXX: This only works for the first constructor; really it should scan for the first constructor it can match
			var cons = type.GetConstructors().FirstOrDefault();
			if (cons == null)
				throw new Exception(
					$"Cannot create {type.Name} via automatic reflection with Dependency Injection. There isn't a single constructor found.");
			var parameters = cons.GetParameters();
			var values = new object[parameters.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				values[i] = provider.GetService(parameters[i].ParameterType);
			}

			var instance = cons?.Invoke(values);
			return instance;
		}

		public IDependencyProviderScope Build()
		{
			return new DependencyProvider(this);
		}

		public IDependencyBuilder RemoveIfExists<T>() => Has<T>() ? Remove<T>() : this;

		public IDependencyBuilder Remove<T>()
		{
			if (TryGetTransient(typeof(T), out var transient))
			{
				TransientServices.Remove(transient);
				return this;
			}

			if (TryGetScoped(typeof(T), out var scoped))
			{
				ScopedServices.Remove(scoped);
				return this;
			}

			if (TryGetSingleton(typeof(T), out var singleton))
			{
				SingletonServices.Remove(singleton);
				return this;
			}

			throw new Exception($"Service does not exist, so cannot be removed. type=[{typeof(T)}]");
		}

		public bool Has<T>()
		{
			return TryGetTransient(typeof(T), out _) || TryGetScoped(typeof(T), out _);
		}

		public bool TryGetTransient(Type type, out ServiceDescriptor descriptor)
		{
			descriptor = TransientServices.FirstOrDefault(s => s.Interface == type);
			return descriptor != null;
		}
		public bool TryGetScoped(Type type, out ServiceDescriptor descriptor)
		{
			descriptor = ScopedServices.FirstOrDefault(s => s.Interface == type);
			return descriptor != null;
		}

		public bool TryGetSingleton(Type type, out ServiceDescriptor descriptor)
		{
			descriptor = SingletonServices.FirstOrDefault(s => s.Interface == type);
			return descriptor != null;
		}


		public IDependencyBuilder Fork()
		{
			return new DependencyBuilder {
				ScopedServices = ScopedServices.ToList(),
				TransientServices = TransientServices.ToList(),
				SingletonServices = SingletonServices.ToList()
			};
		}
	}

}
