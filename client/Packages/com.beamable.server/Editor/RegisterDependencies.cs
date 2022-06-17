using Beamable.Common.Dependencies;
using Beamable.Editor;
using Beamable.Editor.UI.Model;

namespace Beamable.Server.Editor
{
	[BeamContextSystem]
	public class BeamableServerDependencies
	{
		[RegisterBeamableDependencies(-1000, RegistrationOrigin.EDITOR)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.AddSingleton<PublishService>();
			builder.LoadSingleton(provider => new MicroservicesDataModel());
		}
	}
}
