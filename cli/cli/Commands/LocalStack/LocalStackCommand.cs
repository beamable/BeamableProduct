using cli.Services;
using cli.Services.LocalStack;

namespace cli.Commands.LocalStack;

/// <summary>
/// Parent group for the local-stack orchestrator (<c>beam local ...</c>). Brings up a full local
/// Beamable loop — backend, portal, microservices and portal extensions — from a generic,
/// machine-agnostic JSON manifest.
/// </summary>
public class LocalStackCommand : CommandGroup, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse { get; } = true;

	public LocalStackCommand() : base("local", "Orchestrate a full local Beamable stack from a manifest")
	{
	}

	/// <summary>
	/// Resolves the manifest path: the explicit <paramref name="overridePath"/> if given, otherwise
	/// <c>&lt;.beamable&gt;/local-stack.json</c> when a workspace exists, otherwise
	/// <c>local-stack.json</c> in the current working directory.
	/// </summary>
	public static string ResolveManifestPath(ConfigService configService, string overridePath)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
			return Path.GetFullPath(overridePath);

		if (configService?.DirectoryExists == true && !string.IsNullOrEmpty(configService.ConfigDirectoryPath))
			return Path.Combine(configService.ConfigDirectoryPath, LocalStackConfigIO.DefaultFileName);

		return Path.GetFullPath(LocalStackConfigIO.DefaultFileName);
	}

	/// <summary>Resolves the run-state path that sits alongside the resolved manifest.</summary>
	public static string ResolveRunStatePath(ConfigService configService, string overridePath) =>
		LocalStackRunStateIO.ResolveRunStatePath(ResolveManifestPath(configService, overridePath));
}
