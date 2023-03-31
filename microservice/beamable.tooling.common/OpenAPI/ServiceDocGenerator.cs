using Beamable.Server;

namespace Beamable.Tooling.Common.OpenAPI;

public class ServiceDocGenerator
{
	public void Generate(Type microserviceType)
	{
		if (!microserviceType.IsAssignableTo(typeof(Microservice)))
		{
			throw new ArgumentException($"must be a subtype of {nameof(Microservice)}", nameof(microserviceType));
		}
		
		
	}
}
