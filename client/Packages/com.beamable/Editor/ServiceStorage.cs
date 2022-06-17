using Beamable.Common;
using Beamable.Common.Dependencies;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class DependencyExtensions
	{
		public static IDependencyBuilder LoadSingleton<T>(this IDependencyBuilder builder, Func<IDependencyProvider, T> factory)
		{
			builder.AddSingleton<T>(provider =>
			{
				var wrapper = provider.GetService<SingletonStorageWrapper<T>>();
				wrapper.Storage.Apply(wrapper.Service);
				return wrapper.Service;
			});
			builder.AddSingleton(provider => new SingletonStorageWrapper<T>(provider.GetService<ServiceStorage>(), factory(provider)));
			return builder;
		}
	}

	public class SingletonStorageWrapper<T> : IBeamableDisposable
	{
		public ServiceStorage Storage { get; }
		public T Service { get; }

		public SingletonStorageWrapper(ServiceStorage storage, T service)
		{
			Storage = storage;
			Service = service;
		}

		public Promise OnDispose()
		{
			Storage.Save(Service);
			return Promise.Success;
		}
	}

	public class ServiceStorage
	{
		private const string DEFAULT = "__doesn't exist";
		private const string SINGLETON = "singleton";
		private string GetKey<T>(string name)
		{
			return $"s{typeof(T).Name}_n{name}";
		}

		public void Save<T>(T service) => Save(SINGLETON, service);
		public void Save<T>(string name, T service)
		{
			if (service is ISerializationCallbackReceiver receiver)
			{
				receiver.OnBeforeSerialize();
			}
			var json = JsonUtility.ToJson(service);
			SessionState.SetString(GetKey<T>(name), json);
		}

		public void Apply<T>(T service) => Apply(SINGLETON, service);
		public void Apply<T>(string name, T service)
		{
			var json = SessionState.GetString(GetKey<T>(name), DEFAULT);
			if (!string.Equals(DEFAULT, json))
			{
				JsonUtility.FromJsonOverwrite(json, service);
				if (service is ISerializationCallbackReceiver receiver)
				{
					receiver.OnAfterDeserialize();
				}
			}
		}
	}
}
