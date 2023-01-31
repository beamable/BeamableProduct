using Beamable.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Beamable.Server;

public interface ICallableGenerator
{
	List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider);
}

public class FederatedInventoryCallbackGenerator : ICallableGenerator
{
	public List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
	{
		var type = provider.instanceType;
		var output = new List<ServiceMethod>();
		
		var interfaces = type.GetInterfaces();

		foreach (var interfaceType in interfaces)
		{
			if (!interfaceType.IsGenericType) continue;
			if (interfaceType.GetGenericTypeDefinition() != typeof(IFederatedInventory<>)) continue;

			var map = type.GetInterfaceMap(interfaceType);
			var federatedType = interfaceType.GetGenericArguments()[0];
			var identity = Activator.CreateInstance(federatedType) as IThirdPartyCloudIdentity;

			var federatedNamespace = identity.UniqueName;
			for (var i = 0 ; i < map.TargetMethods.Length; i ++)
			{
				var method = map.TargetMethods[i];
				var interfaceMethod = map.InterfaceMethods[i];
				var attribute = method.GetCustomAttribute<CallableAttribute>(true);
				if (attribute != null) continue;

				var path = $"{federatedNamespace}/{interfaceMethod.Name}";
				var tag = federatedNamespace;

				var serviceMethod = ServiceMethodHelper.CreateMethod(
					serviceAttribute,
					provider,
					path,
					tag,
					false,
					new HashSet<string>(),
					method);

				output.Add(serviceMethod);
			}
		}		
		
		return output;
	}
}

public class FederatedLoginCallableGenerator : ICallableGenerator
{
	public List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
	{
		var type = provider.instanceType;
		var output = new List<ServiceMethod>();
		
		var interfaces = type.GetInterfaces();

		foreach (var interfaceType in interfaces)
		{
			if (!interfaceType.IsGenericType) continue;
			if (interfaceType.GetGenericTypeDefinition() != typeof(IFederatedLogin<>)) continue;

			var map = type.GetInterfaceMap(interfaceType);
			var federatedType = interfaceType.GetGenericArguments()[0];
			var identity = Activator.CreateInstance(federatedType) as IThirdPartyCloudIdentity;

			var federatedNamespace = identity.UniqueName;
			var method = map.TargetMethods[0];

			var attribute = method.GetCustomAttribute<CallableAttribute>(true);
			if (attribute != null) continue;
			
			var path = $"{federatedNamespace}/{nameof(IFederatedLogin<DummyThirdParty>.Authenticate)}";
			var tag = federatedNamespace;
			
			var serviceMethod = ServiceMethodHelper.CreateMethod(
				serviceAttribute,
				provider,
				path,
				tag,
				false,
				new HashSet<string>(),
				method);

			output.Add(serviceMethod);
		}		
		
		return output;
	}
}

/// <summary>
/// this class is not meant to be used. It's sole purpose is to stand in
/// when something in the outer class needs to access a method with nameof() 
/// </summary>
class DummyThirdParty : IThirdPartyCloudIdentity
{
	public string UniqueName => "__temp__";
}
