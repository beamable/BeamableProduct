using Beamable.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Beamable.Server;

/// <summary>
/// Represents a generator that scans a type for service methods and generates callable methods.
/// </summary>
public interface ICallableGenerator
{
	/// <summary>
	/// Scans a type for service methods and generates callable methods.
	/// </summary>
	/// <param name="serviceAttribute">The MicroserviceAttribute associated with the microservice.</param>
	/// <param name="provider">The service method provider to scan for methods.</param>
	/// <returns>A list of generated callable service methods.</returns>
	List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider);
}

/// <summary>
/// Generates callable methods for federated inventory functionality.
/// </summary>
public class FederatedInventoryCallbackGenerator : ICallableGenerator
{
	/// <inheritdoc />
	public List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
	{
		var type = provider.instanceType;
		var output = new List<ServiceMethod>();
		
		var interfaces = type.GetInterfaces();

		var methodToPathMap = new Dictionary<string, string>
		{
			[nameof(IFederatedInventory<DummyThirdParty>.GetInventoryState)] = "inventory/state",
			[nameof(IFederatedInventory<DummyThirdParty>.StartInventoryTransaction)] = "inventory/put",
		};
		
		foreach (var interfaceType in interfaces)
		{
			if (!interfaceType.IsGenericType) continue;
			if (interfaceType.GetGenericTypeDefinition() != typeof(IFederatedInventory<>)) continue;

			var map = type.GetInterfaceMap(interfaceType);
			var federatedType = interfaceType.GetGenericArguments()[0];
			var identity = Activator.CreateInstance(federatedType) as IFederationId;

			var federatedNamespace = identity.UniqueName;
			for (var i = 0 ; i < map.TargetMethods.Length; i ++)
			{
				var method = map.TargetMethods[i];
				var interfaceMethod = map.InterfaceMethods[i];
				var attribute = method.GetCustomAttribute<CallableAttribute>(true);
				if (attribute != null) continue;

				if (!methodToPathMap.TryGetValue(interfaceMethod.Name, out var pathName))
				{
					var err = $"Unable to map method name to path part. name=[{interfaceMethod.Name}]";
					throw new Exception(err);
				}
				var path = $"{federatedNamespace}/{pathName}";
				var tag = federatedNamespace;

				var serviceMethod = ServiceMethodHelper.CreateMethod(
					serviceAttribute,
					provider,
					path,
					tag,
					false,
					new HashSet<string>(new []{"*"}),
					method,
					true);

				Log.Debug("Found Federated method. FederatedPath={FederatedPath}, MethodName={MethodName}", path, serviceMethod.Method.Name);
				output.Add(serviceMethod);
			}
		}		
		
		return output;
	}
}

/// <summary>
/// Generates callable methods for federated login functionality.
/// </summary>
public class FederatedLoginCallableGenerator : ICallableGenerator
{
	/// <inheritdoc />
	public List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
	{
		var type = provider.instanceType;
		var output = new List<ServiceMethod>();
		
		var interfaces = type.GetInterfaces();
		var methodToPathMap = new Dictionary<string, string>
		{
			[nameof(IFederatedLogin<DummyThirdParty>.Authenticate)] = "authenticate",
		};

		foreach (var interfaceType in interfaces)
		{
			if (!interfaceType.IsGenericType) continue;
			if (interfaceType.GetGenericTypeDefinition() != typeof(IFederatedLogin<>)) continue;

			var map = type.GetInterfaceMap(interfaceType);
			var federatedType = interfaceType.GetGenericArguments()[0];
			var identity = Activator.CreateInstance(federatedType) as IFederationId;

			var federatedNamespace = identity.UniqueName;
			var method = map.TargetMethods[0];

			var attribute = method.GetCustomAttribute<CallableAttribute>(true);
			if (attribute != null) continue;
			
			if (!methodToPathMap.TryGetValue(map.InterfaceMethods[0].Name, out var pathName))
			{
				var err = $"Unable to map method name to path part. name=[{method.Name}]";
				throw new Exception(err);
			}
			var path = $"{federatedNamespace}/{pathName}";
			var tag = federatedNamespace;
			
			var serviceMethod = ServiceMethodHelper.CreateMethod(
				serviceAttribute,
				provider,
				path,
				tag,
				false,
				new HashSet<string>(new []{"*"}),
				method,
				true);
			
			Log.Debug("Found Federated method. FederatedPath={FederatedPath}, MethodName={MethodName}", path, serviceMethod.Method.Name);
			output.Add(serviceMethod);
		}		
		
		return output;
	}
}

/// <summary>
/// Generates callable methods for federated game server functionality.
/// </summary>
public class FederatedGameServerCallableGenerator : ICallableGenerator
{
    /// <inheritdoc />
	public List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)

	{
		var type = provider.instanceType;
		var output = new List<ServiceMethod>();

		var interfaces = type.GetInterfaces();

		var methodToPathMap = new Dictionary<string, string>
		{
			[nameof(IFederatedGameServer<DummyThirdParty>.CreateGameServer)] = "servers"
		};

		foreach (var interfaceType in interfaces)
		{
			if (!interfaceType.IsGenericType) continue;
			if (interfaceType.GetGenericTypeDefinition() != typeof(IFederatedGameServer<>)) continue;

			var map = type.GetInterfaceMap(interfaceType);
			var federatedType = interfaceType.GetGenericArguments()[0];
			var identity = Activator.CreateInstance(federatedType) as IFederationId;

			var federatedNamespace = identity.UniqueName;
			for (var i = 0 ; i < map.TargetMethods.Length; i ++)
			{
				var method = map.TargetMethods[i];
				var interfaceMethod = map.InterfaceMethods[i];
				var attribute = method.GetCustomAttribute<CallableAttribute>(true);
				if (attribute != null) continue;

				if (!methodToPathMap.TryGetValue(interfaceMethod.Name, out var pathName))
				{
					var err = $"Unable to map method name to path part. name=[{interfaceMethod.Name}]";
					throw new Exception(err);
				}
				var path = $"{federatedNamespace}/{pathName}";
				var tag = federatedNamespace;

				var serviceMethod = ServiceMethodHelper.CreateMethod(
					serviceAttribute,
					provider,
					path,
					tag,
					false,
					new HashSet<string>(new []{"*"}),
					method,
					true);

				Log.Debug("Found Federated method. FederatedPath={FederatedPath}, MethodName={MethodName}", path, serviceMethod.Method.Name);
				output.Add(serviceMethod);
			}
		}

		return output;
	}
}

/// <summary>
/// this class is not meant to be used. It's sole purpose is to stand in
/// when something in the outer class needs to access a method with nameof() 
/// </summary>
class DummyThirdParty : IFederationId
{
	public string UniqueName => "__temp__";
}
