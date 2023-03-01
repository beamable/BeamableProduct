using Beamable.Common.Dependencies;
using Beamable.Server.Editor;

// This will be a part of solana example package
[BeamContextSystem]
public class SolanaAdditionalDependencies
{
	[RegisterBeamableDependencies(-1000, RegistrationOrigin.EDITOR)]
	public static void Register(IDependencyBuilder builder)
	{
		builder.AddSingleton<IMicroserviceBuildHook>(provider => new CustomBuildHook());
	}
}
