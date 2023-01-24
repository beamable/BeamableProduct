using Beamable.Server;

namespace Beamable.tuna3
{
	[Microservice("tuna3", EnableEagerContentLoading = false)]
	public class tuna3 : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b + 1;
		}
	}
}
