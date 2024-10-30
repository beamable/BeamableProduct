using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("LocalPackageLegacyMicroservice")]
	public class LocalPackageLegacyMicroservice : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
