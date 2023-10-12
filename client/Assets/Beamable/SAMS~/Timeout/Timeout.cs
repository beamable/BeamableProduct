using Beamable.Server;

namespace Beamable.Timeout
{
	[Microservice("Time")]
	public class Timeout : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
