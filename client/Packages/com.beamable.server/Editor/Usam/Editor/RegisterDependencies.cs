using Beamable.Common.Dependencies;
using Beamable.Editor;

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
