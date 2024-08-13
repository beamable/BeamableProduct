using Beamable.Common;
using Beamable.Common.Runtime;
using Beamable.Server.Editor;

namespace beamable.tooling.common.Microservice;

public static class FederatedComponentGenerator
{
	public static List<FederationComponent> FindFederatedComponents(Type serviceType)
	{
		var components = new List<FederationComponent>();
		var interfaces = serviceType.GetInterfaces();
		foreach (var it in interfaces)
		{
			if (!it.IsGenericType) continue;

			if (!FederationComponentNames.FederationComponentToName.TryGetValue(it.GetGenericTypeDefinition(),
				    out var typeName))
			{
				continue;
			}

			var federatedType = it.GetGenericArguments()[0];
			if (Activator.CreateInstance(federatedType) is IThirdPartyCloudIdentity identity)
			{
				var component = new FederationComponent
				{
					identity = identity,
					interfaceType = it,
					typeName = typeName
				};
				components.Add(component);
			}
		}

		return components;
	}
}
