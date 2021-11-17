using Beamable.Server;

namespace Beamable.Server
{
	[Microservice("XXXX")]
	public class XXXX : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
