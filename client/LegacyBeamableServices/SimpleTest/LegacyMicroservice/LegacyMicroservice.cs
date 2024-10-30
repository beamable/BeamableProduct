using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("LegacyMicroservice")]
	public class LegacyMicroservice : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
