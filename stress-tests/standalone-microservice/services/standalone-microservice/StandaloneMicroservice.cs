using Beamable.Server;

namespace Beamable.standalone_microservice
{
	[Microservice("standalone_microservice")]
	public class StandaloneMicroservice : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
