using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("LegacyMicroserviceWithFiles")]
	public class LegacyMicroserviceWithFiles : Microservice
	{
		[ClientCallable]
		public int ServerCall(int x)
		{
			var tidbit = new ExtraTidbit
			{
				x = x * 2
			};
			return tidbit.x;
		}
	}
}
