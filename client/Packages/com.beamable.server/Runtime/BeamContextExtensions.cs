
using Beamable.Common.Dependencies;
using Beamable.Server;

namespace Beamable
{
	public static class BeamContextExtensions
	{
		[RegisterBeamableDependencies]
		public static void RegisterServices(IDependencyBuilder builder)
		{
			builder.AddSingleton<MicroserviceClients>();
		}

		public static MicroserviceClients Microservices(this BeamContext ctx) =>
			ctx.ServiceProvider.GetService<MicroserviceClients>();
	}

}
