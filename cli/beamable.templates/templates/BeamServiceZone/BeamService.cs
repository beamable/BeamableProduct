using Beamable.Server;

namespace Beamable.BeamService
{
	// A zone-scoped service runs once per (cid, zid), above realms. It has no player/realm context, so it
	// inherits ZoneMicroservice (not Microservice) and exposes no realm SDK. Clients cannot call it
	// directly; use [ServerCallable] for server-to-server entry points.
	public partial class BeamService : ZoneMicroservice
	{
		[ServerCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
