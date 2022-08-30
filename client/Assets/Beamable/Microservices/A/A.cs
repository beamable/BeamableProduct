using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("A")]
	public class A : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
