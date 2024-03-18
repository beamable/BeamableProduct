using Beamable.Server;

namespace Beamable.standalone_microservice
{
	[Microservice("standalone-microservice", EnableEagerContentLoading = false)]
	public class StandaloneMicroservice : Microservice
	{
		[Callable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
