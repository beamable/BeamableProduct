using Beamable.Server;

namespace Beamable.BeamService
{
	[Microservice("BeamService")]
	public class BeamService : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
