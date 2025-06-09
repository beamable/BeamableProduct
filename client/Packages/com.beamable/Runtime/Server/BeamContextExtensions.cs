using Beamable.Common.Dependencies;
using Beamable.Server;

namespace Beamable
{
	[BeamContextSystem]
	public static class BeamContextExtensions
	{
		public static MicroserviceClients Microservices(this BeamContext ctx) =>
			ctx.ServiceProvider.GetService<MicroserviceClients>();
	}

}
