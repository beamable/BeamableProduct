using Beamable.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.ManagerClient;

namespace Beamable.Server.Editor
{
	[BeamContextSystem]
	public class BeamableServerDependencies
	{
		[RegisterBeamableDependencies(-1000, RegistrationOrigin.EDITOR)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.LoadSingleton(provider => new MicroservicesDataModel(provider.GetService<BeamEditorContext>()));
			builder.AddSingleton<MicroserviceManager>();
			builder.AddSingleton<MicroserviceDiscovery>();
		}

		[RegisterBeamableDependencies(-1000, RegistrationOrigin.RUNTIME)]
		public static void RegisterRuntime(IDependencyBuilder builder)
		{
			builder.AddSingleton<MicroserviceDiscovery>();
			builder.AddSingleton<IMicroservicePrefixService, MicroservicePrefixService>();
		}
	}
}
