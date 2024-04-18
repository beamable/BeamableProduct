using Beamable.Common.Dependencies;
using Beamable.Editor;
using Beamable.Editor.Microservice.UI3;

namespace Beamable.Server.Editor.Usam
{
	[BeamContextSystem]
	public static class RegisterDependencies
	{
		[RegisterBeamableDependencies(-1000, RegistrationOrigin.EDITOR)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.AddSingleton<CodeService>();
			builder.AddSingleton<PublishService>();

			builder.AddGlobalStorage<SamModel, EditorStorageLayer>();
		}

	}
}
