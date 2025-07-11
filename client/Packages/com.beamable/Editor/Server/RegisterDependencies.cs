using Beamable.Api;
using Beamable.Api.Autogenerated.Beamo;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor;
using Beamable.Server.Editor.Usam;

namespace Beamable.Server.Editor
{
	[BeamContextSystem]
	public class BeamableServerDependencies
	{
		/// <summary>
		/// This exists in EDITOR code so it won't be included in a build.
		/// However, the DI is modifying the runtime scope.
		///
		/// That means that the editor experience has different services injected into the
		/// DI scope than a runtime game will. 
		/// </summary>
		/// <param name="builder"></param>
		[RegisterBeamableDependencies(-1000, RegistrationOrigin.RUNTIME | RegistrationOrigin.EDITOR)]
		public static void RegisterRuntime(IDependencyBuilder builder)
		{
			builder.AddSingleton<IServiceRoutingResolution, UsamRoutingResolution>();
			builder.AddSingleton<UsamRoutingStrategy, UsamRoutingStrategy>(_ =>
			{
				var usam = BeamEditorContext.Default.ServiceScope.GetService<UsamService>();
				return new UsamRoutingStrategy(usam);
			});
			builder.AddSingleton<IServiceRoutingStrategy>(p => p.GetService<UsamRoutingStrategy>());
		}
	}
}
