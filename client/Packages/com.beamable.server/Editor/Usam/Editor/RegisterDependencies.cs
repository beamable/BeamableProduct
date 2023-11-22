using Beamable.Common.Dependencies;

namespace Beamable.Server.Editor.Usam
{
	[BeamContextSystem]
	public static class RegisterDependencies
	{
		[RegisterBeamableDependencies(-1000, RegistrationOrigin.EDITOR)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.AddSingleton<CodeService>();
		}

	}
}
