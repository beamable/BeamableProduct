
using Beamable.Common.Dependencies;

namespace Beamable.Server.Editor
{
	[BeamContextSystem]
	public static class BeamEditorContextMicroserviceExtensions
	{
		[RegisterBeamableDependencies(origin: RegistrationOrigin.EDITOR)]
		public static void RegisterServices(IDependencyBuilder builder)
		{
			builder.AddScoped<MicroserviceClients>();
		}

		public static MicroserviceClients Microservices(this BeamEditorContext ctx) =>
			ctx.ServiceScope.GetService<MicroserviceClients>();
	}

}
