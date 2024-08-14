using Beamable.Common.Dependencies;

namespace Beamable.Server
{
	[BeamContextSystem]
	public class RegisterDependencies
	{
		[RegisterBeamableDependencies(origin: RegistrationOrigin.RUNTIME)]
		public static void Register(IDependencyBuilder builder)
		{
			// builder.AddSingleton<IMicroservicePrefixService, RuntimePrefixService>();
		}
	}
}
