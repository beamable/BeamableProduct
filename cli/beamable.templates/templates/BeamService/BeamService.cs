using Beamable.Server;

namespace Beamable.BeamService
{
	public partial class BeamService : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
