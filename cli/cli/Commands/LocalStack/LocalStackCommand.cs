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

		if (configService?.DirectoryExists == true
		    && !string.IsNullOrEmpty(configService.ConfigDirectoryPath)
		    && !IsHomeWorkspace(configService.ConfigDirectoryPath))
			return Path.Combine(configService.ConfigDirectoryPath, LocalStackConfigIO.DefaultFileName);

		// No workspace was registered at startup. Prefer a manifest inside a `.beamable` folder in the current
		// directory — that is where `init` writes one, and the folder can have been created AFTER the ConfigService
		// was built (by an `init` in this same shell), so DirectoryExists above would not have seen it.
		var localWorkspaceManifest = Path.GetFullPath(
			Path.Combine(ConfigService.CFG_FOLDER, LocalStackConfigIO.DefaultFileName));
		if (File.Exists(localWorkspaceManifest))
			return localWorkspaceManifest;

		// Finally, a bare manifest sitting in the current directory (how it worked before `.beamable` was the default).
		return Path.GetFullPath(LocalStackConfigIO.DefaultFileName);
	}

	/// <summary>
	/// Where <c>beam local init</c> should WRITE a manifest. Same as <see cref="ResolveManifestPath"/> except that,
	/// with no workspace, it targets <c>&lt;cwd&gt;/.beamable/local-stack.json</c> rather than a bare
	/// <c>local-stack.json</c> — so a fresh directory gets a proper workspace folder instead of a loose file that
	/// the rest of the CLI does not recognise as one.
	///
	/// The directory is not created here; <see cref="EnsureManifestDirectory"/> does that, so the caller can
	/// confirm first.
	/// </summary>
	public static string ResolveManifestPathForInit(ConfigService configService, string overridePath)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
			return Path.GetFullPath(overridePath);

		if (configService?.DirectoryExists == true
		    && !string.IsNullOrEmpty(configService.ConfigDirectoryPath)
		    && !IsHomeWorkspace(configService.ConfigDirectoryPath))
			return Path.Combine(configService.ConfigDirectoryPath, LocalStackConfigIO.DefaultFileName);

		var workingDirectory = configService?.WorkingDirectory ?? Directory.GetCurrentDirectory();
		return Path.GetFullPath(
			Path.Combine(workingDirectory, ConfigService.CFG_FOLDER, LocalStackConfigIO.DefaultFileName));
	}

	/// <summary>
	/// Makes sure the directory a manifest is about to be written into exists, creating it when the caller allows.
	/// Returns false when it is missing and creation was declined.
	///
	/// <paramref name="confirm"/> is invoked only when the directory does not exist yet, so an existing workspace
	/// is never questioned. Creating a <c>.beamable</c> folder is a visible side effect in someone's project, so
	/// interactive callers ask first; quiet ones create it.
	/// </summary>
	public static bool EnsureManifestDirectory(string manifestPath, Func<string, bool> confirm, out string directory)
	{
		directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
		if (string.IsNullOrEmpty(directory) || Directory.Exists(directory))
			return true;

		if (confirm != null && !confirm(directory))
			return false;

		Directory.CreateDirectory(directory);
		return true;
	}

	/// <summary>
	/// True when a resolved workspace is <c>~/.beamable</c> — i.e. the HOME directory itself.
	///
	/// Workspace lookup walks up from the working directory, so running a command outside any workspace lands on
	/// the home directory whenever a <c>~/.beamable</c> exists. That is never what someone means: it silently
	/// writes a <c>local-stack.json</c> (and config, and logs) into their home folder, and every later invocation
	/// from an unrelated directory then picks that phantom workspace up instead of reporting that there is none.
	///
	/// Treating it as "no workspace" makes the local-stack commands use a manifest in the CURRENT directory
	/// instead, which is both predictable and what the fallback below already did before a <c>~/.beamable</c>
	/// happened to exist.
	/// </summary>
	public static bool IsHomeWorkspace(string configDirectoryPath)
	{
		try
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty(home)) return false;

			var resolved = Path.GetFullPath(configDirectoryPath).TrimEnd(Path.DirectorySeparatorChar);
			var homeWorkspace = Path.Combine(home, ConfigService.CFG_FOLDER).TrimEnd(Path.DirectorySeparatorChar);

			return string.Equals(resolved, homeWorkspace,
				OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Resolves the run-state path that sits alongside the resolved manifest.</summary>
	public static string ResolveRunStatePath(ConfigService configService, string overridePath) =>
		LocalStackRunStateIO.ResolveRunStatePath(ResolveManifestPath(configService, overridePath));

	/// <summary>The well-known checkout folder names the local stack spans, in <c>(name, kind)</c> form.</summary>
	public const string ApiRepoName = "BeamableAPI";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string ScalaRepoName = "BeamableBackend";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string PortalRepoName = "agentic-portal";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string ProductRepoName = "BeamableProduct";

	/// <summary>
	/// Looks for a folder named <paramref name="name"/> in <paramref name="startDir"/> and its ancestors (up to
	/// <paramref name="maxLevels"/> levels up), returning the first match — used to auto-fill the repo paths
	/// (BeamableAPI / BeamableBackend / agentic-portal) that typically sit as siblings a level or two up.
	///
	/// Shared by <c>init</c> and <c>setup</c>: setup has to find the checkouts on its own, because it is meant to
	/// run BEFORE a manifest exists and therefore cannot read the paths from one.
	/// </summary>
	public static string FindRepoDir(string startDir, string name, int maxLevels = 3)
	{
		try
		{
			var dir = new DirectoryInfo(string.IsNullOrEmpty(startDir) ? Directory.GetCurrentDirectory() : startDir);
			for (var i = 0; i <= maxLevels && dir != null; i++)
			{
				var candidate = Path.Combine(dir.FullName, name);
				if (Directory.Exists(candidate)) return candidate;
				dir = dir.Parent;
			}
		}
		catch { /* best-effort discovery */ }

		return null;
	}
}
